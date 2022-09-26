using System.Linq;
using FluentAssertions;
using Nyx.Cli.Rendering;
using Xunit;

namespace Nyx.Cli.Tests;

public class PropertyReaderBuilderTests
{
    class SubItem
    {
        
    }
    
    class Item
    {
        public string StringProp { get; set; }
        
        public int IntProp { get; set; }
        
        public SubItem SubItem { get; set; }
        
        public string[] ArrayOfString { get; set; }
    }
    
    [Fact]
    public void TestBuildingMetadata()
    {
        var fetchers = PropertyReaderBuilder.GetMetadataFromReflection<Item>();

        fetchers.Should().HaveCount(3);
    }
    
    [Fact]
    public void TestBuildingMetadata2()
    {
        var fetchers = PropertyReaderBuilder.GetMetadataFromReflection<Item>();

        var map = fetchers.ToDictionary(x => x.propertyName, x => x.propertyFetcher);
        var testItem = new Item()
        {
            ArrayOfString = new[] { "subitem1", "subitem2" }
        };
        var result = map["ArrayOfString"](testItem);

        result.Should().BeSameAs(testItem.ArrayOfString);
        
    }
}