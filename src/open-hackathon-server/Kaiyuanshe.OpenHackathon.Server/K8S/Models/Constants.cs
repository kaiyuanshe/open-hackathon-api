namespace Kaiyuanshe.OpenHackathon.Server.K8S.Models
{
    public static class Namespaces
    {
        public const string Default = "default";
    }

    public static class Labels
    {
        public const string TemplateId = "templateId";
        public const string HackathonName = "hackathonName";
        public const string UserId = "userId";
    }

    public static class Kinds
    {
        public const string Experiment = "Experiment";
        public const string Template = "Template";
    }

    public static class EnvNames
    {
        public const string User = "USER";
    }

    public static class CustomResourceDefinition
    {
        //CRD: https://github.com/kaiyuanshe/cloudengine/blob/master/config/crd/bases/hackathon.kaiyuanshe.cn_experiments.yaml
        // group/version can be found in above link. The same values for template/experiment
        public const string Group = "hackathon.kaiyuanshe.cn";
        public const string Version = "v1";
        public const string ApiVersion = "hackathon.kaiyuanshe.cn/v1";

        public static class Plurals
        {
            // Properties of CustomResourceDefinition
            // See also: https://github.com/kaiyuanshe/cloudengine/blob/master/config/crd/bases/hackathon.kaiyuanshe.cn_templates.yaml
            public const string Templates = "templates";

            // Properties of CustomResourceDefinition
            // See also: https://github.com/kaiyuanshe/cloudengine/blob/master/config/crd/bases/hackathon.kaiyuanshe.cn_experiments.yaml
            public const string Experiments = "experiments";
        }
    }
}
