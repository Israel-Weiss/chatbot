﻿using BasePlugin.Interfaces;
using BasePlugin.Records;
using Dal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    class CallbacksProxy : ICallbacks
    {
        public Action StartSession { get; set; }
        public Action EndSession { get; set; }
    }

    public class PluginExecutor
    {
        const string SESSION_PLUGIN_ID = "SESSION_PLUGIN_ID";

        private readonly IDal _dal;
        private readonly PluginsMenu _pluginsMenu;
        private readonly PluginsManager _pluginsManager;

        public PluginExecutor(IDal dal, PluginsMenu pluginsMenu, PluginsManager pluginsManager)
        {
            _dal = dal;
            _pluginsMenu = pluginsMenu;
            _pluginsManager = pluginsManager;
        }

        public string Run(string message, string user)
        {
            var currentPluginId = _dal.LoadData(user, SESSION_PLUGIN_ID);
            if (currentPluginId == null)
            {
                if (message.ToLower() == "help")
                {
                    return _pluginsMenu.PlaginsHelp();
                }

                var pluginIdFromUser = message.Split(' ')[0];

                if (!int.TryParse(pluginIdFromUser, out int pluginNumber))
                {
                    return "This option is not recognized, please type help to see the options.";
                }

                if (pluginNumber > PluginsManager.plugins.Count || pluginNumber <= 0)
                {
                    return $"You only allowed to press number between 1 and {PluginsManager.plugins.Count}.";
                }

                var msg = String.Join(' ', message.Split(' ').Skip(1).ToList());

                var pluginId = PluginsManager.plugins[pluginNumber - 1];
                return Execute(pluginId, msg, user);
            }
            else
            {
                return Execute(currentPluginId, message, user);
            }
        }

        private string Execute(string pluginId, string input, string user)
        {
            var callbacks = new CallbacksProxy
            {
                StartSession = () => _dal.SaveData(user, SESSION_PLUGIN_ID, pluginId),
                EndSession = () => _dal.SaveData(user, SESSION_PLUGIN_ID, null)
            };

            var plugin = _pluginsManager.CreatePlugin(pluginId);
            var persistentData = _dal.LoadData(user, pluginId);
            var output = plugin.Execute(new PluginInput(input, persistentData, callbacks));

            _dal.SaveData(user, pluginId, output.PersistentData);
            return output.Message;
        }
    }
}