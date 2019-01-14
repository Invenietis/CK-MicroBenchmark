using NUnit.Framework;
using System;
using CK.Core;
using FluentAssertions;
using System.Linq;

namespace CK.MicroBenchmark.Tests
{
    [TestFixture]
    public class MicroBenchmarkTests
    {
        [Test]
        public void BenchmarkResult_is_correct()
        {
            int i = 0;
            BenchmarkResult r = CK.Core.MicroBenchmark.MeasureTime( () => ++i );
            r.Average.Should().Be( r.Timings.Average() );
            r.MaxTiming.Should().Be( r.Timings.Max() );
            r.MinTiming.Should().Be( r.Timings.Min() );
            r.StandardDeviation.Should().Be( Math.Sqrt( r.Timings.Average( z => z * z ) - Math.Pow( r.Timings.Average(), 2 ) ) );
            r.MeanAbsoluteDeviation.Should().Be( r.Timings.Select( t => Math.Abs(t - r.Timings.Average()) ).Sum() / r.Timings.Count );
            r.NormalizedMean.Should().Be( r.Timings.Where( t => t < r.Average + r.MeanAbsoluteDeviation ).Sum() / r.Timings.Where( t => t < r.Average + r.MeanAbsoluteDeviation ).Count() );
            i.Should().Be( 1 + 5 * 10000 );
        }

        [Test]
        public void Benchmarking_timed_operations()
        {
            int Fib( int n )
            {
                return n <= 0 ? n : Fib( n - 1 ) + Fib( n - 2 );
            }
            BenchmarkResult r1 = CK.Core.MicroBenchmark.MeasureTime( () => Fib( 9 ) );
            BenchmarkResult r2 = CK.Core.MicroBenchmark.MeasureTime( () => Fib( 11 ) );
            r1.IsBetterThan( r2 ).Should().BeTrue();
            r1.IsSignificantlyBetterThan( r2 ).Should().BeTrue();
            r1.IsTotallyBetterThan( r2 ).Should().BeTrue();
        }

        [Test]
        public void Benchmarking_CPU_is_less_precise_that_time()
        {
            int Fib( int n )
            {
                return n <= 0 ? n : Fib( n - 1 ) + Fib( n - 2 );
            }
            BenchmarkResult r1 = CK.Core.MicroBenchmark.MeasureCPU( () => Fib( 10 ) );
            BenchmarkResult r2 = CK.Core.MicroBenchmark.MeasureCPU( () => Fib( 15 ) );
            r1.IsBetterThan( r2 ).Should().BeTrue();
            r1.IsSignificantlyBetterThan( r2 ).Should().BeTrue();
            r1.IsTotallyBetterThan( r2 ).Should().BeTrue();
        }

        [Test]
        public void Benchmarking_for_loop_on_array_shows_that_foreach_is_the_winner()
        {
            var values = Enumerable.Range( 1, 5000 ).ToArray();

            BenchmarkResult rLinqCount = CK.Core.MicroBenchmark.MeasureTime( () =>
            {
                long sum = 0;
                for( int i = 0; i < values.Count(); ++i )
                {
                    sum += values[i];
                }
            } );
            BenchmarkResult rLength = CK.Core.MicroBenchmark.MeasureTime( () =>
            {
                long sum = 0;
                for( int i = 0; i < values.Length; ++i )
                {
                    sum += values[i];
                }
            } );
            BenchmarkResult rForEach = CK.Core.MicroBenchmark.MeasureTime( () =>
            {
                long sum = 0;
                foreach( int i in values )
                {
                    sum += i;
                }
            } );
            rLength.IsTotallyBetterThan( rLinqCount ).Should().BeTrue( "Count() is ovviously a perf killer." );
            rForEach.IsTotallyBetterThan( rLinqCount ).Should().BeTrue( "Count() is ovviously a perf killer." );
            rForEach.IsBetterThan( rLength ).Should().BeTrue( "foreach is better than for( i < Length )." );
        }

    }
}
