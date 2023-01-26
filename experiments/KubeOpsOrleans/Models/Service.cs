using KubeOpsOrleans.Crd;
using Nyx.Utils.Collections;

namespace KubeOpsOrleans.Models;

public record Service(string Name, Uri Uri);

public record ConfigurationRenderingContext(HomerSettings Settings, ValueCollection<Service> Services);