using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Captures the result of <see cref="MicroBenchmark.MeasureCPU"/>
    /// and <see cref="MicroBenchmark.MeasureTime"/>.
    /// </summary>
    public class BenchmarkResult
    {
        /// <summary>
        /// The raw timings obtained. 
        /// </summary>
        public readonly IReadOnlyList<double> Timings;

        /// <summary>
        /// The minimal benchmark time obtained.
        /// </summary>
        public readonly double MinTiming;

        /// <summary>
        /// The maximal benchmark time obtained.
        /// </summary>
        public readonly double MaxTiming;

        /// <summary>
        /// The average of the <see cref="Timings"/>.
        /// </summary>
        public readonly double Average;

        /// <summary>
        /// The standard deviation of the <see cref="Timings"/>.
        /// </summary>
        public readonly double StandardDeviation;

        /// <summary>
        /// The mean absolute deviation (also called average absolute deviation),
        /// see <see cref="https://en.wikipedia.org/wiki/Average_absolute_deviation"/>/
        /// The mean absolute deviation from the mean is less than or equal to the <see cref="StandardDeviation"/>.
        /// </summary>
        public readonly double MeanAbsoluteDeviation;

        /// <summary>
        /// The normalized mean is the mean of the <see cref="Timings"/> that are
        /// lower than <see cref="Average"/> + <see cref="MeanAbsoluteDeviation"/>: "big" timings
        /// are considered as noise and are excluded.
        /// </summary>
        public readonly double NormalizedMean;

        /// <summary>
        /// Initializes a new <see cref="BenchmarkResult"/> with an array of at least 2 measures.
        /// </summary>
        /// <param name="timings">The timings. There must be at least 2 measures.</param>
        public BenchmarkResult( double[] timings )
        {
            if( timings == null || timings.Length < 2 ) throw new ArgumentException();
            double sum = 0;
            double sumSquare = 0;
            double minTiming = double.MaxValue;
            double maxTiming = 0;
            for( int i = 0; i < timings.Length; ++i )
            {
                var t = timings[i];
                sum += t;
                sumSquare += t * t;
                if( minTiming > t ) minTiming = t;
                if( maxTiming < t ) maxTiming = t;
            }
            double average = sum / timings.Length;
            double stdDev = Math.Sqrt( sumSquare / timings.Length - average * average );
            double[] deviations = new double[timings.Length];
            for( int i = 0; i < timings.Length; ++i )
            {
                deviations[i] = average - timings[i];
            }
            double meanDeviation = 0;
            for( int i = 0; i < timings.Length; ++i )
            {
                meanDeviation += Math.Abs( deviations[i] );
            }
            meanDeviation /= timings.Length;
            double normalizedMean = 0;
            int normalizedMeanCount = 0;
            for( int i = 0; i < timings.Length; ++i )
            {
                if( deviations[i] > 0 || -deviations[i] <= meanDeviation )
                {
                    normalizedMean += timings[i];
                    ++normalizedMeanCount;
                }
            }
            normalizedMean /= normalizedMeanCount;
            //
            Timings = timings;
            MinTiming = minTiming;
            MaxTiming = maxTiming;
            Average = average;
            StandardDeviation = stdDev;
            MeanAbsoluteDeviation = meanDeviation;
            NormalizedMean = normalizedMean;
        }

        /// <summary>
        /// Uses <see cref="NormalizedMean"/> to test whether this result is better than the other one.
        /// </summary>
        /// <param name="other">Other result. Must not be null.</param>
        /// <returns>True if this result is better than the other one.</returns>
        public bool IsBetterThan( BenchmarkResult other )
        {
            if( other == null ) throw new ArgumentNullException( nameof( other ) );
            return NormalizedMean < other.NormalizedMean;
        }

        /// <summary>
        /// Uses <see cref="NormalizedMean"/> and <see cref="StandardDeviation"/> to test whether this
        /// result is significantly better than the other one.
        /// </summary>
        /// <param name="other">Other result. Must not be null.</param>
        /// <returns>True if this result is better than the other one.</returns>
        public bool IsSignificantlyBetterThan( BenchmarkResult other )
        {
            if( other == null ) throw new ArgumentNullException( nameof( other ) );
            return (NormalizedMean + StandardDeviation / 2) < (other.NormalizedMean - other.StandardDeviation / 2);
        }

        /// <summary>
        /// This result is totally better than the other one if its worst timing (<see cref="MaxTiming"/>)
        /// is still better than the best other's timing (<see cref="MinTiming"/>).
        /// </summary>
        /// <param name="other">Other result. Must not be null.</param>
        /// <returns>True if this result is better than the other one.</returns>
        public bool IsTotallyBetterThan( BenchmarkResult other )
        {
            if( other == null ) throw new ArgumentNullException( nameof( other ) );
            return MaxTiming < other.MinTiming;
        }

        public override string ToString()
        {
            return $"NormalizedMean: {NormalizedMean}, Min: {MinTiming}, Max: {MaxTiming}, Average: {Average}, StandardDeviation: {StandardDeviation}, {Timings.Count} measures.";
        }
    }

}
