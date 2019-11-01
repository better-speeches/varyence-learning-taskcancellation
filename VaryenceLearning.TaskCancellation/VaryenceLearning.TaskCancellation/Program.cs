﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace VaryenceLearning.TaskCancellation
{
    internal static class Program
    {
        private static void Main()
        {
            ExecuteTaskAsync().Wait();

            //ExecuteTaskWithTimeoutAsync(TimeSpan.FromSeconds(2)).Wait();
            //ExecuteTaskWithTimeoutAsync(TimeSpan.FromSeconds(6)).Wait();

            //ExecuteManuallyCancellableTaskAsync().Wait();
            //ExecuteManuallyCancellableTaskAsync().Wait();

            //CancelANonCancellableTaskAsync().Wait();
        }

        private static async Task ExecuteTaskAsync()
        {
            Console.WriteLine(nameof(ExecuteTaskAsync));
            Console.WriteLine("Result {0}", await LongRunningOperation(100));
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();
        }

        private static async Task ExecuteTaskWithTimeoutAsync(TimeSpan timeSpan)
        {
            Console.WriteLine(nameof(ExecuteTaskWithTimeoutAsync));

            using (var cancellationTokenSource = new CancellationTokenSource(timeSpan))
            {
                try
                {
                    var result = await LongRunningCancellableOperation(500, cancellationTokenSource.Token);
                    Console.WriteLine("Result {0}", result);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Task was cancelled");
                }
            }
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();
        }

        private static async Task ExecuteManuallyCancellableTaskAsync()
        {
            Console.WriteLine(nameof(ExecuteManuallyCancellableTaskAsync));

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                // Creating a task to listen to keyboard key press
                var keyBoardTask = Task.Run(() =>
                {
                    Console.WriteLine("Press enter to cancel");
                    Console.ReadKey();

                    // Cancel the task
                    cancellationTokenSource.Cancel();
                });

                try
                {
                    var longRunningTask = LongRunningCancellableOperation(500, cancellationTokenSource.Token);

                    var result = await longRunningTask;
                    Console.WriteLine("Result {0}", result);
                    Console.WriteLine("Press enter to continue");
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Task was cancelled");
                }

                await keyBoardTask;
            }
        }

        private static async Task CancelANonCancellableTaskAsync()
        {
            Console.WriteLine(nameof(CancelANonCancellableTaskAsync));

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                // Listening to key press to cancel
                var keyBoardTask = Task.Run(() =>
                {
                    Console.WriteLine("Press enter to cancel");
                    Console.ReadKey();

                    // Sending the cancellation message
                    cancellationTokenSource.Cancel();
                });

                try
                {
                    // Running the long running task
                    var longRunningTask = LongRunningOperationWithCancellationTokenAsync(200, cancellationTokenSource.Token);
                    var result = await longRunningTask;

                    Console.WriteLine("Result {0}", result);
                    Console.WriteLine("Press enter to continue");
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Task was cancelled");
                }

                await keyBoardTask;
            }
        }
        
        private static Task<decimal> LongRunningOperation(int loop)
        {
            // Start a task a return it
            return Task.Run(() =>
            {
                decimal result = 0;

                // Loop for a defined number of iterations
                for (int i = 0; i < loop; i++)
                {
                    // Do something that takes times like a Thread.Sleep in .NET Core 2.
                    Thread.Sleep(10);
                    result += i;
                }

                return result;
            });
        }

        private static Task<decimal> LongRunningCancellableOperation(int loop, 
            CancellationToken cancellationToken)
        {
            Task<decimal> task = null;

            // Start a task a return it
            task = Task.Run(() =>
            {
                decimal result = 0;

                // Loop for a defined number of iterations
                for (var i = 0; i < loop; i++)
                {
                    // Check if a cancellation is requested, if yes,
                    // throw a TaskCanceledException.
                    if (cancellationToken.IsCancellationRequested)
                        throw new TaskCanceledException(task);

                    cancellationToken.ThrowIfCancellationRequested();

                    // Do something that takes times like a Thread.Sleep in .NET Core 2.
                    Thread.Sleep(10);
                    result += i;
                }

                return result;
            }, cancellationToken);

            return task;
        }
        
        private static async Task<decimal> LongRunningOperationWithCancellationTokenAsync(int loop, 
            CancellationToken cancellationToken)
        {
            // We create a TaskCompletionSource of decimal
            var taskCompletionSource = new TaskCompletionSource<decimal>();

            // Registering a lambda into the cancellationToken
            cancellationToken.Register(() =>
            {
                // We received a cancellation message, cancel the TaskCompletionSource.Task
                taskCompletionSource.TrySetCanceled();
            });

            var task = LongRunningOperation(loop);

            // Wait for the first task to finish among the two
            var completedTask = await Task.WhenAny(task, taskCompletionSource.Task);

            return await completedTask;
            
            if (completedTask == task)
            {
                // Extract the result, the task is finished and the await will return immediately
                var result = await task;

                // Set the taskCompletionSource result
                taskCompletionSource.TrySetResult(result);
            }

            // Return the result of the TaskCompletionSource.Task
            return await taskCompletionSource.Task;
        }
    }
}