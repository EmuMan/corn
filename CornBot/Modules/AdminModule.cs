using Discord.Interactions;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CornBot.Utilities;
using CornBot.Models;

namespace CornBot.Modules
{

    public class AdminModule : InteractionModuleBase<SocketInteractionContext>
    {

        public InteractionService? Commands { get; set; }
        private readonly IServiceProvider _services;

        public AdminModule(IServiceProvider services)
        {
            _services = services;
            _services.GetRequiredService<CornClient>().Log(
                LogSeverity.Debug, "Modules", "Creating GeneralModule...");
        }

    }
}
