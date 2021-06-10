﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Web.Portal.Data.Infrastructure;
using Web.Portal.Model.Models;

namespace Web.Portal.Data.Repositories
{
    public interface INotifyAWBRepository : IRepository<NotifyAWB>
    {
    }
    public class NotifyAWBRepository : RepositoryFlightControlBase<NotifyAWB>, INotifyAWBRepository
    {
        public NotifyAWBRepository(IDbFactory dbFactory) : base(dbFactory)
        {
        }
    }
}
