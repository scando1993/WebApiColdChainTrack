using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;

namespace ColdChainTrack.Auth.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public string IdNumber { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public DateTime? DayOfBirth { get; set; }
        public string Gender { get; set; }
        public string ProfileImage { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreationDate { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager, string authenticationType)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, authenticationType);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection")
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public virtual DbSet<ApplicationUserLog> UserLogs { get; set; }
        public virtual DbSet<Tracking> Trackings { get; set; }
        public virtual DbSet<Device> Devices { get; set; }
        public virtual DbSet<SigfoxMessage> SigfoxMessages { get; set; }
    }

    public class ApplicationUserLog
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public string MacAddress { get; set; }
        public string DeviceId { get; set; }
        public DateTime? CreationDateTime { get; set; }
        [ForeignKey("UserId")]
        virtual public ApplicationUser User { get; set; }
    }

    [Table("Tracking")]
    public class Tracking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime Dtm { get; set; }
        public int DeviceIdDevice { get; set; }
        public string Location { get; set; }
        public int Temperature { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [ForeignKey("DeviceIdDevice")]
        public virtual Device Device { get; set; }
    }

    [Table("Devices")]
    public class Device
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdDevice { get; set; }
        public string Family { get; set; }
        public string Name { get; set; }
    }

    [Table("SigfoxMessages")]
    public class SigfoxMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int SeqNumber { get; set; }
        public int msgId { get; set; }
        public string device { get; set; }
        public int Frame { get; set; }
        //public long time { get; set; }
        public DateTime time { get; set; }
        public String payload1 { get; set; }
        public String payload2 { get; set; }
    }
}