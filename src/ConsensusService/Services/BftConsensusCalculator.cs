using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsensusService.Services
{
    public record SensorVote(string SensorId, double Value);

    public record ConsensusResult(
        double? ConsensusValue,
        int ParticipatingSensors,
        List<string> OutlierSensorIds);

    public class BftConsensusCalculator
    {
        private const double OutlierThresholdMultiplier = 2.0;

        public ConsensusResult Calculate(List<SensorVote> votes)
        {
            if (votes.Count == 0)
                return new ConsensusResult(null, 0, new List<string>());

            var values = votes.Select(v => v.Value).OrderBy(v => v).ToList();
            var median = Median(values);

            if (votes.Count < 3)
            {
                // Not enough independent votes to detect outliers meaningfully.
                return new ConsensusResult(median, votes.Count, new List<string>());
            }

            var deviations = votes
                .Select(v => Math.Abs(v.Value - median))
                .OrderBy(d => d)
                .ToList();
            var mad = Median(deviations);

            // Avoid a zero-MAD edge case (all values identical) which would flag every entry as an outlier.
            var threshold = mad == 0 ? double.MaxValue : mad * OutlierThresholdMultiplier;

            var outliers = votes
                .Where(v => Math.Abs(v.Value - median) > threshold)
                .Select(v => v.SensorId)
                .ToList();

            var inliers = votes.Where(v => !outliers.Contains(v.SensorId)).ToList();

            var consensusValue = inliers.Count > 0
                ? Median(inliers.Select(v => v.Value).OrderBy(v => v).ToList())
                : median; // fallback: everyone disagrees, use raw median anyway

            return new ConsensusResult(consensusValue, inliers.Count, outliers);
        }

        private static double Median(List<double> sortedValues)
        {
            int n = sortedValues.Count;
            if (n % 2 == 1)
                return sortedValues[n / 2];
            return (sortedValues[n / 2 - 1] + sortedValues[n / 2]) / 2.0;
        }
    }
}
