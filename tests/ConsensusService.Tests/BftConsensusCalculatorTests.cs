namespace ConsensusService.Tests;

using ConsensusService.Services;

public class BftConsensusCalculatorTests
{
    private readonly BftConsensusCalculator _calculator = new();

    [Fact]
    public void NoVotes_ReturnsNullConsensus()
    {
        var result = _calculator.Calculate(new List<SensorVote>());
        Assert.Null(result.ConsensusValue);
        Assert.Equal(0, result.ParticipatingSensors);
    }

    [Fact]
    public void TwoIdenticalVotes_ReturnsThatValue_NoOutliers()
    {
        var votes = new List<SensorVote>
        {
            new("s1", 50.0),
            new("s2", 50.0)
        };
        var result = _calculator.Calculate(votes);
        Assert.Equal(50.0, result.ConsensusValue);
        Assert.Empty(result.OutlierSensorIds);
    }

    [Fact]
    public void AllIdenticalVotes_NoOutliersFlagged()
    {
        var votes = new List<SensorVote>
        {
            new("s1", 50.0), new("s2", 50.0), new("s3", 50.0),
            new("s4", 50.0), new("s5", 50.0)
        };
        var result = _calculator.Calculate(votes);
        Assert.Equal(50.0, result.ConsensusValue);
        Assert.Empty(result.OutlierSensorIds);
    }

    [Fact]
    public void OneOutlierAmongFive_IsDetectedAndExcluded()
    {
        var votes = new List<SensorVote>
        {
            new("s1", 50.0), new("s2", 51.0), new("s3", 49.5),
            new("s4", 50.5), new("s5", 200.0) // malicious
        };
        var result = _calculator.Calculate(votes);
        Assert.Contains("s5", result.OutlierSensorIds);
        Assert.Equal(4, result.ParticipatingSensors);
        Assert.True(result.ConsensusValue < 60); // not dragged toward 200
    }

    [Fact]
    public void AllVotesDisagree_FallsBackToRawMedian()
    {
        var votes = new List<SensorVote>
        {
            new("s1", 10.0), new("s2", 90.0), new("s3", 200.0)
        };
        var result = _calculator.Calculate(votes);
        Assert.NotNull(result.ConsensusValue);
    }

    [Fact]
    public void FewerThanThreeVotes_SkipsOutlierDetection()
    {
        var votes = new List<SensorVote> { new("s1", 10.0), new("s2", 500.0) };
        var result = _calculator.Calculate(votes);
        Assert.Empty(result.OutlierSensorIds); // can't tell who's lying with only 2
    }
}
