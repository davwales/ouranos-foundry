using GdUnit4;
using Ouranos.Foundry.Core.Utils;
using Shouldly;

namespace Ouranos.Foundry.Tests.Core.Utils;

[TestSuite]
public class StableIdTests
{
    [TestCase]
    public void Create_SameInputs_ReturnsSameId()
    {
        StableId.Create(42, "region", 3).ShouldBe(StableId.Create(42, "region", 3));
    }

    [TestCase]
    public void Create_KnownInputs_ReturnsPinnedValue()
    {
        // Pins the derivation so a change that would invalidate saved IDs fails loudly.
        StableId
            .Create(42, "region", 0)
            .ShouldBe(new Guid("5e380d2c-29cb-b7c2-2034-a01f8d8c0399"));
    }

    [TestCase(42UL, "region", 0UL, 43UL, "region", 0UL)]
    [TestCase(42UL, "region", 0UL, 42UL, "city", 0UL)]
    [TestCase(42UL, "region", 0UL, 42UL, "region", 1UL)]
    public void Create_DifferentInputs_ReturnsDifferentIds(
        ulong seedA,
        string kindA,
        ulong indexA,
        ulong seedB,
        string kindB,
        ulong indexB
    )
    {
        StableId.Create(seedA, kindA, indexA).ShouldNotBe(StableId.Create(seedB, kindB, indexB));
    }

    [TestCase]
    public void Create_ManyIndices_ProducesNoCollisions()
    {
        var ids = new HashSet<Guid>();

        for (ulong seed = 0; seed < 10; seed++)
        {
            for (ulong i = 0; i < 1000; i++)
            {
                ids.Add(StableId.Create(seed, "city", i)).ShouldBeTrue();
            }
        }
    }

    [TestCase]
    public void Create_FromParent_IsDeterministicAndDistinctFromParent()
    {
        var parent = StableId.Create(7, "city", 0);

        var child = StableId.Create(parent, "poi", 0);

        child.ShouldBe(StableId.Create(parent, "poi", 0));
        child.ShouldNotBe(parent);
        child.ShouldNotBe(StableId.Create(parent, "poi", 1));
        child.ShouldNotBe(StableId.Create(StableId.Create(7, "city", 1), "poi", 0));
    }

    [TestCase]
    public void Create_FromPair_DependsOnOrder()
    {
        var a = StableId.Create(1, "city", 0);
        var b = StableId.Create(1, "city", 1);

        StableId.Create(a, "road", b).ShouldBe(StableId.Create(a, "road", b));
        StableId.Create(a, "road", b).ShouldNotBe(StableId.Create(b, "road", a));
    }
}
