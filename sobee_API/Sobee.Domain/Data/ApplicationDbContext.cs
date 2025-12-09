using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Identity;

namespace Sobee.Domain.Data {
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser> {
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options) {
		}
	}
}
