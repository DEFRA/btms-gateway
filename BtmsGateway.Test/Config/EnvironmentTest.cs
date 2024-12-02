using Microsoft.AspNetCore.Builder;

namespace BtmsGateway.Test.Config;

public class EnvironmentTest
{
   [Fact]
   public void IsNotDevModeByDefault()
   {
      var builder = WebApplication.CreateBuilder();

      var isDev = BtmsGateway.Config.Environment.IsDevMode(builder);

      Assert.False(isDev);
   }
}
