using Microsoft.AspNetCore.Builder;

namespace BtmsGateway.Test.Config;

public class EnvironmentTest
{

   [Fact]
   public void IsNotDevModeByDefault()
   {
      var _builder = WebApplication.CreateBuilder();

      var isDev = BtmsGateway.Config.Environment.IsDevMode(_builder);

      Assert.False(isDev);
   }
}
