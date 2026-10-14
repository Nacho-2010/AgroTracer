using Xunit;
using AgroTraceCR.Infrastructure;
public class HashTests {
 [Fact] public void HashIsStableAndDetectsChanges(){var h=new HashService();Assert.Equal(h.Hash(new {schema=1,codigo="BOV-CR-0001"}),h.Hash(new {schema=1,codigo="BOV-CR-0001"}));Assert.NotEqual(h.Hash(new {codigo="BOV-CR-0001"}),h.Hash(new {codigo="BOV-CR-0002"}));Assert.Equal(64,h.Hash(new {id=1}).Length);}
}
