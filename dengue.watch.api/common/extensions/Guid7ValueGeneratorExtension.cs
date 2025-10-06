using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace dengue.watch.api.common.extensions;

public class Guid7ValueGeneratorExtension : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => throw new NotImplementedException();

    public override Guid Next(EntityEntry entry)
    {
        return Guid.CreateVersion7();
    }
}