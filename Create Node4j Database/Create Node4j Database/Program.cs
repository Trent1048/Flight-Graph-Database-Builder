using Neo4j.Driver;

public class HelloWorldExample : IDisposable
{
	private readonly IDriver _driver;

	public HelloWorldExample(string uri, string user, string password)
	{
		_driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
	}

	public async void RunCypher(string cypher)
	{
		using var session = _driver.AsyncSession();
		await session.ExecuteWriteAsync(
			async tx => {
				await tx.RunAsync(cypher);
			}
		);
    }

	public void Dispose()
	{
		_driver?.Dispose();
	}

	public void FlushDB() 
	{
		RunCypher(
			@"MATCH (n)
			DETACH DELETE n"
		);
	}

	public void CreateNode(List<string> line) 
	{
		RunCypher(
			$@"CREATE (a:Airport {{id: {line[0]}, name: ""{line[1]}"", city: ""{line[2]}"", country: ""{line[3]}""}})"
		);
	}

	public string CreateRelation(List<string> line) 
	{
		RunCypher(
			$@"MATCH (source:Airport {{id: {line[3]}}}), (dest:Airport {{id: {line[5]}}})
			CREATE (source)-[:ROUTE]->(dest)"	
		);
		return "";
	}

	public static void Main()
	{
		bool quickModeEnabled = true;
		if (quickModeEnabled) 
		{
			Console.WriteLine("Running with quick mode enabled. Will only load the first 1000 airports and routes that connect them.");
		}

        using var greeter = new HelloWorldExample("bolt://localhost:7687", "neo4j", "password");
		Console.WriteLine("Flushing DB...");
        greeter.FlushDB();
		Console.WriteLine("DB flush completed.\n");

		Console.WriteLine("Beginning DB rebuild...");

		Console.WriteLine("Adding airport nodes...");
		Console.Write("                     ]\r[");
		int progressCounter = 0;
		HashSet<string> safeIDs = new HashSet<string>();
        foreach (string line in File.ReadLines("./airports.csv")) 
		{
			List<string> tokens = line.Replace("\"", "").Split(',').ToList();
			greeter.CreateNode(tokens);

			progressCounter++;
			if (progressCounter % ((quickModeEnabled ? 300 : 7698) / 20) == 0) 
			{
				Console.Write("█");
			}

			if (quickModeEnabled) 
			{
				safeIDs.Add(tokens[0]);
			}

			if (quickModeEnabled && progressCounter >= 300)
				break;
		}
		Console.WriteLine("]\nAirport nodes added.");

        Console.WriteLine("Adding route relations...");
        Console.Write("                     ]\r[");
        progressCounter = 0;
        foreach (string line in File.ReadLines("./routes.csv"))
        {
            List<string> tokens = line.Replace("\"", "").Split(',').ToList();

            progressCounter++;
            if (progressCounter % (67663 / 20) == 0)
            {
                Console.Write("█");
            }

            if (tokens[3] == "\\N" || tokens[5] == "\\N")
				continue;

			if (quickModeEnabled && (!safeIDs.Contains(tokens[3]) || !safeIDs.Contains(tokens[5])))
				continue;

            greeter.CreateRelation(tokens);
        }
        Console.WriteLine("]\nRoute relations added.");

        Console.WriteLine("Database rebuild complete.");



		Console.ReadLine();
	}
}

// helpful links
// https://neo4j.com/docs/dotnet-manual/current/get-started/
// https://neo4j.com/docs/dotnet-manual/current/cypher-workflow/
// https://neo4j.com/docs/api/dotnet-driver/current/html/f7d9eea1-5357-f1de-8a54-336a77141b6d.htm
// https://neo4j.com/docs/cypher-manual/current/clauses/create/