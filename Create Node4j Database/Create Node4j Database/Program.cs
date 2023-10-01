using Neo4j.Driver;

public class HelloWorldExample : IDisposable
{
	private readonly IDriver _driver;

	public HelloWorldExample(string uri, string user, string password)
	{
		_driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
	}

	//Refactor into generic RunCypher function
	public async void PrintGreeting(string message)
	{
		using var session = _driver.AsyncSession();
		var greeting = await session.ExecuteWriteAsync(
			async tx => {
				var result = await tx.RunAsync(
					"CREATE (a:Company)" + 
					"SET a.message = 'Hello World'" +
					"RETURN a.message + ', from node ' + id(a)"
					);
					return (await result.SingleAsync())[0].As<string>();
			}
		);

        Console.WriteLine(greeting);
    }

	public void Dispose()
	{
		_driver?.Dispose();
	}

	/*CLEANUP FUNCTION DELETES EVERYTHING*/
	public void FlushDB() 
	{
		//DELETE STUFF
	}

	public string CreateNode(List<string> line) 
	{
		//CREATE ONE NODE FROM CSV FILE
		return "";
	}

	public string CreateRelation(List<string> line) 
	{
		//CREATE ONE RELATION FROM CSV
		return "";
	}

	public static void Main()
	{
		//Open CSVs
		//Create each node
		//Create each link
		//Profit

		using var greeter = new HelloWorldExample("bolt://localhost:7687", "neo4j", "password");
		greeter.PrintGreeting("hello, world");
		Console.ReadLine();
	}
}

// helpful links
// https://neo4j.com/docs/dotnet-manual/current/get-started/
// https://neo4j.com/docs/dotnet-manual/current/cypher-workflow/
// https://neo4j.com/docs/api/dotnet-driver/current/html/f7d9eea1-5357-f1de-8a54-336a77141b6d.htm
// https://neo4j.com/docs/cypher-manual/current/clauses/create/