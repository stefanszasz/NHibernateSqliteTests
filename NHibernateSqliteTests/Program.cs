using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Instances;
using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace NHibernateSqliteTests
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            NHibernateConfig config = new NHibernateConfig();
            config.Session.Save(new Customer {Name = "ioio"});
            config.Session.Flush();
        }
    }

    public class NHibernateConfig
    {
        private Configuration configuration;
        public ISession Session { get; set; }

        public NHibernateConfig()
        {
            var config = new SQLiteConfiguration()
        .InMemory()
        .ShowSql()
        .Raw("hibernate.generate_statistics", "true");

            var nhConfig = Fluently.Configure()
                    .Database(config)
                    .Mappings(mappings =>
                         mappings.FluentMappings.AddFromAssemblyOf<CustomerMap>()
                    .Conventions.AddFromAssemblyOf<IdGenerationConvention>())
                    .ExposeConfiguration(x => new SchemaExport(x).Execute(true, true, false));

            var SessionSource = new SessionSource(nhConfig);
            Session = SessionSource.CreateSession();
            SessionSource.BuildSchema(Session);

            // run the deployment script
            var deploymentScriptQuery = Session.CreateSQLQuery("ALTER TABLE hibernate_unique_key ADD COLUMN TableKey VARCHAR(255); INSERT INTO hibernate_unique_key (TableKey, next_hi) values ('Document', 1);");
            deploymentScriptQuery.ExecuteUpdate();    
        }        
    }

    public class IdGenerationConvention : IIdConvention
    {
        public void Apply(IIdentityInstance instance)
        {
            var where = string.Format("TableKey = '{0}'", instance.EntityType.Name);
            instance.GeneratedBy.HiLo("1000");
        }
    }

    public class Customer
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }

    public class CustomerMap : ClassMap<Customer>
    {
        public CustomerMap()
        {
            Id(x => x.Id);
            Map(x => x.Name);
        }
    }
}