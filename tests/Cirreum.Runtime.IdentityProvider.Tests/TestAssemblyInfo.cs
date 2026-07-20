// The deferred log queue asserted by these tests is process-global; parallel test
// classes writing while another enumerates would race. Run classes sequentially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
