using Neo4j.Driver;

public class AirportDataIngestor : IDisposable
{
	private readonly IDriver _driver;

	public AirportDataIngestor(string uri, string user, string password)
	{
		_driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
	}

    public void Dispose()
    {
        _driver?.Dispose();
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

	public void FlushDB() 
	{
		RunCypher(
			@"MATCH (n)
			DETACH DELETE n"
		);
	}

	public void CreateAirport(List<string> line) 
	{
		RunCypher(
			$@"CREATE (a:Airport {{id: {line[0]}, name: ""{line[1]}"", city: ""{line[2]}"", country: ""{line[3]}"", iata: ""{line[4]}"", iaco: ""{line[5]}"", latitude: {line[6]}, longitude: {line[7]}, altitude: {line[8]}, timezone: ""{line[9]}"", dst: ""{line[10]}"", tztime: ""{line[11]}"",type: ""{line[12]}"", source: ""{line[13]}""}})"
		);
	}

	public string CreateRoute(List<string> line) 
	{
		//NOTE: Mostly fill in the blank here, but airline ID's with the value \N are replaced with -1 so that the field can be stored as an int.
		RunCypher(
			$@"MATCH (source:Airport {{id: {line[3]}}}), (dest:Airport {{id: {line[5]}}})
			CREATE (source)-[:ROUTE {{airline: ""{line[0]}"", airlineid: {(line[1] == "\\N" ? -1 : line[1])}, source: ""{line[2]}"", sourceid: {line[3]}, dest: ""{line[4]}"", destid: {line[5]}, codeshare: ""{line[6]}"", stops: {line[7]}, equipment: ""{line[8]}""}}]->(dest)"	
		);
		return "";
	}

	public static void Main()
	{
		//If true, will only load the first 300 airports and routes that connect them. Makes things way faster for testing.
		bool quickModeEnabled = false;
		if (quickModeEnabled) 
		{
			Console.WriteLine("Running with quick mode enabled. Will only load the first 1000 airports and routes that connect them.");
		}

        using var greeter = new AirportDataIngestor("bolt://localhost:7687", "neo4j", "password");

		Console.WriteLine("Flushing DB...");
        greeter.FlushDB();
		Console.WriteLine("DB flush completed.\n");

		Console.WriteLine("Beginning DB rebuild...");

		Console.WriteLine("Adding airport nodes...");
		Console.Write("                     ]\r[");
		int progressCounter = 0;
		HashSet<string> safeIDs = new HashSet<string>(); //Only used by quick mode to track what airports are in the first 1000
        foreach (string line in File.ReadLines("./airports.csv")) 
		{
			List<string> tokens = line.Replace("\"", "").Split(',').ToList(); //All " characters are removed to avoid issues with string formatting later.
			//Accounts for the edge case in which airport names contain a comma, which splits the second entry into two.
			if (tokens.Count > 14) 
			{
				tokens.Insert(1, $"{tokens[1]} {tokens[2]}");
				tokens.RemoveRange(2, 2);
			}
			greeter.CreateAirport(tokens);

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
				break; //Early exit for quick mode
		}
		Console.WriteLine("]\nAirport nodes added.");

        Console.WriteLine("Adding route relations...");
        Console.Write("                     ]\r[");
        progressCounter = 0;
        foreach (string line in File.ReadLines("./routes.csv"))
        {
            List<string> tokens = line.Replace("\"", "").Split(',').ToList(); //" Characters also removed from this input to make string formatting work.

            progressCounter++;
            if (progressCounter % (67663 / 20) == 0)
            {
                Console.Write("█");
            }

            if (tokens[3] == "\\N" || tokens[5] == "\\N") //Skip any records that don't have a valid airport ID for either their source or destination
				continue;

			if (quickModeEnabled && (!safeIDs.Contains(tokens[3]) || !safeIDs.Contains(tokens[5])))
				continue; //In safe mode, skips the slow step if the route isn't between two airports in the first 1000

            greeter.CreateRoute(tokens);
        }
        Console.WriteLine("]\nRoute relations added.");

        Console.WriteLine("Database rebuild complete.");	}
}