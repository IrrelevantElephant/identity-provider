using Microsoft.EntityFrameworkCore;

namespace Database;

public class ApplicationContext(DbContextOptions<ApplicationContext> options) : DbContext(options)
{
}