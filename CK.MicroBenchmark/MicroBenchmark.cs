using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace CK.Core
{
    /// <summary>
    /// From https://stackoverflow.com/questions/969290/exact-time-measurement-for-performance-testing.
    /// This class offers 2 kind of measure: <see cref="MeasureCPU"/> and <see cref="MeasureTime"/>.
    /// </summary>
    public static class MicroBenchmark
    {
        interface IStopwatch : IDisposable
        {
            TimeSpan Elapsed { get; }
            void Start();
            void Stop();
            void Reset();
        }

        class TimeWatch : IStopwatch
        {
            Stopwatch _stopwatch = new Stopwatch();
            ThreadPriority _threadPriority;
            IntPtr _currentAffinity;
            ProcessPriorityClass _processPrority;

            public TimeWatch()
            {
                if( !Stopwatch.IsHighResolution )
                    throw new NotSupportedException( "Your hardware doesn't support high resolution counter" );

                // Prevents the JIT Compiler from optimizing Fkt calls away
                long seed = Environment.TickCount;
                // Uses the second Core/Processor for the test.
                var p = Process.GetCurrentProcess();
                _currentAffinity = p.ProcessorAffinity;
                p.ProcessorAffinity = new IntPtr( 2 );
                // Prevents "Normal" Processes from interrupting Threads
                _processPrority = p.PriorityClass;
                p.PriorityClass = ProcessPriorityClass.High;
                // Prevents "Normal" Threads from interrupting this thread
                _threadPriority = Thread.CurrentThread.Priority;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            }

            public TimeSpan Elapsed => _stopwatch.Elapsed;

            public void Start() => _stopwatch.Start();

            public void Stop() => _stopwatch.Stop();

            public void Reset() => _stopwatch.Reset();

            public void Dispose()
            {
                var p = Process.GetCurrentProcess();
                p.ProcessorAffinity = _currentAffinity;
                p.PriorityClass = _processPrority;
                Thread.CurrentThread.Priority = _threadPriority;
            }
        }

        class CPUWatch : IStopwatch
        {
            readonly Process _p;
            TimeSpan _startTime;
            TimeSpan _endTime;

            public CPUWatch()
            {
                _p = Process.GetCurrentProcess();
            }

            public TimeSpan Elapsed => _endTime - _startTime;

            public void Start()
            {
                _startTime = Process.GetCurrentProcess().TotalProcessorTime;
            }

            public void Stop()
            {
                _endTime = Process.GetCurrentProcess().TotalProcessorTime;
            }

            public void Reset()
            {
                _startTime = TimeSpan.Zero;
                _endTime = TimeSpan.Zero;
            }

            public void Dispose()
            {
            }
        }

        /// <summary>
        /// Computes a <see cref="BenchmarkResult"/> for an action using time measurements (milli seconds).
        /// The action will be executed <paramref name="warmupCount"/> + <paramref name="timingCount"/> * <see cref="iterations"/> times
        /// </summary>
        /// <param name="action">The code to execute. Can not be null.</param>
        /// <param name="iterations">The number of times action will be executed. Must be at least 1.</param>
        /// <param name="timingCount">The number of sampling. Must be at least 2.</param>
        /// <param name="warmupCount">
        /// The number of times the action will be executed before the actual benchmarked executions.
        /// Must be greater or equal to 0.
        /// </param>
        /// <returns>The benchmark result.</returns>
        public static BenchmarkResult MeasureTime( Action action, int iterations = 10000, int timingCount = 5, int warmupCount = 1 )
        {
            return Benchmark<TimeWatch>( action, iterations, timingCount, warmupCount );
        }

        /// <summary>
        /// Computes a <see cref="BenchmarkResult"/> for an action using CPU processing time.
        /// The action will be executed <paramref name="warmupCount"/> + <paramref name="timingCount"/> * <see cref="iterations"/> times
        /// </summary>
        /// <param name="action">The code to execute. Can not be null.</param>
        /// <param name="iterations">The number of times action will be executed. Must be at least 1.</param>
        /// <param name="timingCount">The number of sampling. Must be at least 2.</param>
        /// <param name="warmupCount">
        /// The number of times the action will be executed before the actual benchmarked executions.
        /// Must be greater or equal to 0.
        /// </param>
        /// <returns>The benchmark result.</returns>
        public static BenchmarkResult MeasureCPU( Action action, int iterations = 10000, int timingCount = 5, int warmupCount = 1 )
        {
            return Benchmark<CPUWatch>( action, iterations, timingCount, warmupCount );
        }

        static BenchmarkResult Benchmark<T>( Action action, int iterations, int timingCount, int warmupCount ) where T : IStopwatch, new()
        {
            if( action == null ) throw new ArgumentNullException( nameof( action ) );
            if( iterations <= 0 ) throw new ArgumentException( "Must be positive.", nameof( iterations ) );
            if( timingCount < 2 ) throw new ArgumentException( "Must be 2 or more.", nameof( timingCount ) );
            if( warmupCount < 0 ) throw new ArgumentException( "Must be greater or equal to 0.", nameof( warmupCount ) );
            // Clean Garbage.
            GC.Collect();
            // Wait for the finalizer queue to empty.
            GC.WaitForPendingFinalizers();
            // Clean Garbage.
            GC.Collect();
            // Wait for the finalizer queue to empty.
            GC.WaitForPendingFinalizers();
            var timings = new double[timingCount];
            // Warm up
            while( --warmupCount >= 0 ) action();
            using( var stopwatch = new T() )
            {
                for( int i = 0; i < timingCount; i++ )
                {
                    stopwatch.Reset();
                    stopwatch.Start();
                    for( int j = 0; j < iterations; j++ ) action();
                    stopwatch.Stop();
                    timings[i] = stopwatch.Elapsed.TotalMilliseconds;
                }
            }
            return new BenchmarkResult( timings );
        }

    }
}

