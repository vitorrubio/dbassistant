using Microsoft.EntityFrameworkCore;

namespace DBAssistant.Data.Persistence;

public sealed class FinTechXDbContext : DbContext
{
    public FinTechXDbContext(DbContextOptions<FinTechXDbContext> options)
        : base(options)
    {
    }
}
