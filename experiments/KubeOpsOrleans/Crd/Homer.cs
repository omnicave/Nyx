using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOpsOrleans.Crd;

[KubernetesEntity(Group = "nyx.io", ApiVersion = "v1beta", Kind = "homer", PluralName = "homers")]
public class HomerV1Beta : CustomKubernetesEntity<HomerSpec, HomerStatus>
{
    
}

public class HomerStatus
{
    public DateTime? LastConfigUpdate { get; set; }
    public DateTime? LastDeploymentRestart { get; set; }
    public DateTime? LastReconcileTime { get; set; }
}

public record HomerSettings(
    string Title = "",
    string Subtitle = "",
    bool ShowHeader = true,
    int Columns = 3,
    bool ConnectivityCheck = true
);

// public class HomerStatus
// {
// }
//
// public class HomerSettings
// {
//     public string Title { get; set; } = string.Empty;
//     
//     public string Subtitle { get; set; } = string.Empty;
//
//     public bool ShowHeader { get; set; } = true;
//
//     public int Columns { get; set; } = 3;
//
//     public bool ConnectivityCheck { get; set; } = true;
// }
//
public class HomerSpec
{
    public int? Replicas { get; set; }

    public bool IsDefault { get; set; } = false;

    public HomerSettings Settings { get; set; } = new();
    
    public V1PodTemplateSpec? PodTemplate { get; set; }
}