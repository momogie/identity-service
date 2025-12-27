namespace Shared;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeEndpoint : Attribute
{
    public string FeatureId { get; set; }

    public string[] Features { get; set; }

    public AuthorizationFeatureMethod Method { get; set; }

    public AuthorizeEndpoint(string featureId)
    {
        FeatureId = featureId;
        Method = AuthorizationFeatureMethod.Default;
    }

    public AuthorizeEndpoint(AuthorizationFeatureMethod method, params string[] features)
    {
        Features = features;
        Method = method;
    }
}

public enum AuthorizationFeatureMethod
{
    Default,
    And,
    Or
}
