# Discovering Silo Addresses and Port from Separate Client

## Ideas

- `Nyx.Orleans.Host` can have an `ApiController` providing the information required to use `UseStaticClustering()` on the `ClientBuilder`.
- Use Kubernetes constructs to discover the silo: provide a headless service name that points to all the pods of the cluster. 