﻿using imageStacker.Core.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace imageStacker.Core
{
    public abstract class ThreadProcessingStrategy<T> : ProcessingStrategy<T> where T : IProcessableImage
    {
        public ThreadProcessingStrategy(ILogger logger, IMutableImageFactory<T> factory) : base(logger, factory)
        { }

        protected readonly IBoundedQueue<T> inputQueue = BoundedQueueFactory.Get<T>(16);
        protected readonly IBoundedQueue<(T image, ISaveInfo saveInfo)> outputQueue = BoundedQueueFactory.Get<(T image, ISaveInfo saveInfo)>(8);

        protected async Task ReadingThread(IImageReader<T> reader)
        {
            try
            {
                await reader.Produce(inputQueue);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                throw;
            }
        }
        protected async Task WritingThread(IImageWriter<T> writer)
        {
            try
            {
                var tasks = new Queue<Task>();
                while (!outputQueue.IsCompleted)
                {
                    logger.NotifyFillstate(outputQueue.Count, "WriteBuffer");

                    var imageInfo = await outputQueue.Dequeue();

                    tasks.Enqueue(Task.Run(() => writer.WriteFile(imageInfo.image, imageInfo.saveInfo)));

                    if (tasks.Count > 16)
                        await tasks.Dequeue();
                }
                await Task.WhenAll(tasks);
                logger.NotifyFillstate(outputQueue.Count, "WriteBuffer");
                logger.WriteLine("Processing Ended", Verbosity.Warning, true);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                throw;
            }
        }

        protected async Task<T> GetFirstImage()
        {
            while (!inputQueue.IsCompleted)
            {
                var firstData = await inputQueue.Dequeue();
                return firstData;
            }

            throw new InvalidOperationException("No Image could be found");
        }

        protected abstract Task ProcessingThread(List<IFilter<T>> filter);

        public override async Task Process(IImageReader<T> reader, List<IFilter<T>> filters, IImageWriter<T> writer)
        {
            try
            {
                var readThread = Task.Run(() => ReadingThread(reader));
                var processThread = Task.Run(() => ProcessingThread(filters));
                var writeThread = Task.Run(() => WritingThread(writer));

                await Task.WhenAll(readThread, processThread, writeThread);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
