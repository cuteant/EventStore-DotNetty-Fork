
namespace EventStore.ClientAPI.Resilience
{
  internal static class RetryExtensions
  {
    public static Retries Retries(this int input) => (Retries)input;
  }
}
