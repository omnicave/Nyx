namespace Nyx.Orleans.Jobs;

public record JobEvent();

public record JobChangedStatusEvent(JobStatus Previous, JobStatus Current) : JobEvent();