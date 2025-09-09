using Microsoft.Extensions.Options;

namespace BtmsGateway.Config;

public static class OptionsBuilderExtensions
{
    public static OptionsBuilder<T> ValidateOptions<T>(this OptionsBuilder<T> builder, bool validateOnStart = true)
        where T : class
    {
        return validateOnStart
            ? builder.ValidateDataAnnotations().ValidateOnStart()
            : builder.ValidateDataAnnotations();
    }
}
