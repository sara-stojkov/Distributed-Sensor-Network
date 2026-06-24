# Byzantine Fault Tolerance (BFT) Consensus Approach

## Problem
The system has 5 active sensors at any time. Some sensors may be malicious:
they may stop responding, respond late, or send incorrect (out-of-range or
manipulated) values. The system must compute a single consensus temperature
value per minute that is not corrupted by faulty sensors, and must identify
which sensors are misbehaving.

## Why not full PBFT
Classical BFT protocols (PBFT, etc.) solve agreement on state/order across
nodes that must replicate the same decision, using multiple communication
rounds between nodes. Our problem is simpler: we are not asking sensors
to agree with each other, we are asking the server to derive a trustworthy
value from independent, possibly-corrupted measurements. This maps onto
statistical outlier rejection rather than a multi-round agreement protocol.

## Chosen approach: median-based voting with outlier rejection
1. Collect all GOOD-quality readings from the past minute, one set per sensor.
2. Compute the median of all submitted values. Median is used instead of
   mean because it is robust to outliers - a single extreme malicious value
   cannot drag the median far, whereas it can drag a mean arbitrarily far.
3. For each sensor's reading, compute its absolute deviation from the median.
4. Any reading deviating more than a threshold (e.g. 2x the median absolute
   deviation, MAD) is flagged as an outlier.
5. The consensus value is the median of the non-outlier readings.
6. Sensors whose readings are repeatedly flagged as outliers across cycles
   are marked Quality = BAD and excluded from future consensus rounds.

## Fault tolerance bound
Classical BFT requires n >= 3f + 1 to guarantee correctness, where f is the
number of faulty nodes. With n = 5, f <= 1 is the value with a formal
guarantee. Our median-based heuristic will often catch more than one
outlier in practice, but only f = 1 is provably safe under adversarial
conditions where colluding sensors could otherwise shift the median.

## Limitations
- This is a simplified, single-round heuristic, not a full Byzantine
  agreement protocol; it assumes faulty sensors act independently rather
  than colluding to shift the median together.
- A malicious sensor that sends consistently small/plausible deviations
  may not be detected, since it stays within the outlier threshold.