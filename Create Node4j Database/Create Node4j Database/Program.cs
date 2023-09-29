using Neo4j.Driver;

public class HelloWorldExample : IDisposable
{
	private readonly IDriver _driver;

	public HelloWorldExample(string uri, string user, string password)
	{
		_driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
	}

	public void PrintGreeting(string message)
	{
		using var session = _driver.AsyncSession();
		var greeting = session.ExecuteWriteAsync(
		tx => {

				return tx.RunAsync("CREATE (a:Company {name: $message})", new { message });

				//var result = tx.RunAsync(
				//	"CREATE (a:Greeting) " +
				//	"SET a.message = $message " +
				//	"RETURN a.message + ', from node ' + id(a)",
				//	new { message });
				//
				//return result;
				//return result.Single()[0].As<string>();
			});

		Console.WriteLine("AAAAAAAAAHHHHHHHHHHHHHHHHH");
		Console.WriteLine(greeting);
		Console.WriteLine("AAAAAAAAAHHHHHHHHHHHHHHHHH 2");
	}

	public void Dispose()
	{
		_driver?.Dispose();
	}

	public static void Main()
	{
		using var greeter = new HelloWorldExample("bolt://localhost:7687", "neo4j", "password");

		greeter.PrintGreeting("hello, world");
	}
}

// helpful links
// https://neo4j.com/docs/dotnet-manual/current/get-started/
// https://neo4j.com/docs/dotnet-manual/current/cypher-workflow/
// https://neo4j.com/docs/api/dotnet-driver/current/html/f7d9eea1-5357-f1de-8a54-336a77141b6d.htm
// https://neo4j.com/docs/cypher-manual/current/clauses/create/