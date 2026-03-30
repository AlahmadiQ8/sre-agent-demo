using Microsoft.AspNetCore.Mvc.Testing;

namespace ContosoBank.Tests.Integration;

[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<WebApplicationFactory<Program>>;
