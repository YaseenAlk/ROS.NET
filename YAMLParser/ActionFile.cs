﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using YAMLParser;

namespace FauxMessages
{
    public class ActionFile
    {
        public string Name { get; private set; }
        public MsgsFile GoalMessage { get; private set; }
        public MsgsFile ResultMessage { get; private set; }
        public MsgsFile FeedbackMessage { get; private set; }
        public MsgsFile GoalActionMessage { get; private set; }
        public MsgsFile ResultActionMessage { get; private set; }
        public MsgsFile FeedbackActionMessage { get; private set; }

        private string fileNamespace = "Messages";
        private List<SingleType> stuff = new List<SingleType>();
        private string className;
        private MsgFileLocation MsgFileLocation;
        private List<string> linesOfActionFile = new List<string>();


        public ActionFile(MsgFileLocation filename)
        {
            // Read in action file
            string[] lines = File.ReadAllLines(filename.Path);
            InitializeActionFile(filename, lines);
        }


        public ActionFile(MsgFileLocation filename, string[] lines)
        {
            InitializeActionFile(filename, lines);
        }


        public void ParseAndResolveTypes()
        {
            GoalMessage.ParseAndResolveTypes();
            GoalActionMessage.ParseAndResolveTypes();
            ResultMessage.ParseAndResolveTypes();
            ResultActionMessage.ParseAndResolveTypes();
            FeedbackMessage.ParseAndResolveTypes();
            FeedbackActionMessage.ParseAndResolveTypes();
        }


        public void Write(string outdir)
        {
            string[] chunks = Name.Split('.');
            for (int i = 0; i < chunks.Length - 1; i++)
                outdir = Path.Combine(outdir, chunks[i]);
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);
            string contents = GenerateActionMessages();
            if (contents != null)
                File.WriteAllText(Path.Combine(outdir, MsgFileLocation.basename + "ActionMessages.cs"),
                    contents.Replace("FauxMessages", "Messages")
                );
        }


        /// <summary>
        /// Loads the template for a single message and replaces all the $placeholders with appropriate content
        /// </summary>
        public string GenerateMessageFromTemplate(MsgsFile message)
        {
            string template = Templates.ActionMessageTemplate;
            var properties = message.GenerateProperties();
            template = template.Replace("$CLASS_NAME", message.classname);
            template = template.Replace("$$PROPERTIES", properties);
            template = template.Replace("$ISMETA", message.meta.ToString().ToLower());
            template = template.Replace("$MSGTYPE", "MsgTypes." + fileNamespace.Replace("Messages.", "") + "__" + message.classname);
            template = template.Replace("$MESSAGEDEFINITION", "@\"" + message.Definition + "\"");
            template = template.Replace("$HASHEADER", message.HasHeader.ToString().ToLower());
            template = template.Replace("$NULLCONSTBODY", "");
            template = template.Replace("$EXTRACONSTRUCTOR", "");

            template = template.Replace("$MD5SUM", MD5.Sum(message));

            string deserializationCode = "";
            string serializationCode = "";
            string randomizationCode = "";
            string equalizationCode = "";
            for (int i = 0; i < message.Stuff.Count; i++)
            {
                deserializationCode += message.GenerateDeserializationCode(message.Stuff[i], 1);
                serializationCode += message.GenerateSerializationCode(message.Stuff[i], 1);
                randomizationCode += message.GenerateRandomizationCode(message.Stuff[i], 1);
                equalizationCode += message.GenerateEqualityCode(message.Stuff[i], 1);
            }

            template = template.Replace("$SERIALIZATIONCODE", serializationCode);
            template = template.Replace("$DESERIALIZATIONCODE", deserializationCode);
            template = template.Replace("$RANDOMIZATIONCODE", randomizationCode);
            template = template.Replace("$EQUALITYCODE", equalizationCode);

            return template;
        }


        /// <summary>
        /// Loads the template for the action message class, which holds all six action messages and inserts the code for each
        /// message class.
        /// </summary>
        /// <returns></returns>
        public string GenerateActionMessages()
        {
            var template = Templates.ActionMessagesPlaceHolder;
            template = template.Replace("$NAMESPACE", GoalMessage.Package);
            var messages = new List<MsgsFile> { GoalMessage, GoalActionMessage, ResultMessage, ResultActionMessage,
                FeedbackMessage, FeedbackActionMessage
            };
            var placeHolders = new List<string> { "$GOAL_MESSAGE", "$ACTION_GOAL_MESSAGE", "$RESULT_MESSAGE",
                "$ACTION_RESULT_MESSAGE", "$FEEDBACK_MESSAGE", "$ACTION_FEEDBACK_MESSAGE"
            };

            for (int i = 0; i < messages.Count; i++)
            {
                var generatedCode = GenerateMessageFromTemplate(messages[i]);
                template = template.Replace(placeHolders[i], generatedCode);
            }

            return template;
        }


        /// <summary>
        /// Wrapper to create a MsgsFile
        /// </summary>
        private MsgsFile CreateMessageFile(MsgFileLocation messageLocation, List<string> parameters, string suffix)
        {
            var result = new MsgsFile(new MsgFileLocation(
                messageLocation.Path, messageLocation.searchroot),
                parameters,
                suffix
            );

            return result;
        }


        private void InitializeActionFile(MsgFileLocation filename, string[] lines)
        {
            MsgFileLocation = filename;
            Name = filename.package + "." + filename.basename;
            className = filename.basename;
            fileNamespace += "." + filename.package;

            var parsedAction = ParseActionFile(lines);

            // Goal Messages
            GoalMessage = CreateMessageFile(filename, parsedAction.GoalParameters, "Goal");
            GoalActionMessage = CreateMessageFile(filename, parsedAction.GoalActionParameters, "ActionGoal");


            // Result Messages
            ResultMessage = CreateMessageFile(filename, parsedAction.ResultParameters, "Result");
            ResultActionMessage = CreateMessageFile(filename, parsedAction.ResultActionParameters, "ActionResult");

            // Feedback Messages
            FeedbackMessage = CreateMessageFile(filename, parsedAction.FeedbackParameters, "Feedback");
            FeedbackActionMessage = CreateMessageFile(filename, parsedAction.FeedbackActionParameters, "ActionFeedback");
        }


        /// <summary>
        /// Extracts and generates the parameters for the six messages that are needed to use the actionlib, i.e. Goal,
        /// ActionGoal, Result, ActionResult, Feedback, ActionFeedback. The Action parameters are the ones that are generated.
        /// </summary>
        /// <param name="lines">The content of the .action file</param>
        /// <returns>A ValueTuple with the parameters in a different field</returns>
        private (List<string> GoalParameters, List<string> ResultParameters, List<string> FeedbackParameters,
            List<string> GoalActionParameters, List<string> ResultActionParameters, List<string> FeedbackActionParameters)
            ParseActionFile (string[] lines)
        {
            var goalParameters = new List<string>();
            var resultParameters = new List<string>();
            var feedbackParameters = new List<string>();
            var goalActionParameters = new List<string>();
            var resultActionParameters = new List<string>();
            var feedbackActionParameters = new List<string>();

            linesOfActionFile = new List<string>();
            int foundDelimeters = 0;

            // Search through for the "---" separator between request and response
            for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                lines[lineNumber] = lines[lineNumber].Replace("\"", "\\\"");
                if (lines[lineNumber].Contains('#'))
                {
                    lines[lineNumber] = lines[lineNumber].Substring(0, lines[lineNumber].IndexOf('#'));
                }
                lines[lineNumber] = lines[lineNumber].Trim();

                if (lines[lineNumber].Length == 0)
                {
                    continue;
                }
                linesOfActionFile.Add(lines[lineNumber]);

                if (lines[lineNumber].Contains("---"))
                {
                    foundDelimeters += 1;
                }

                if (foundDelimeters == 0)
                {
                    if (goalActionParameters.Count == 0)
                    {
                        goalActionParameters.Add("Header header");
                        goalActionParameters.Add("actionlib_msgs/GoalID goal_id");
                        goalActionParameters.Add($"{Name.Replace(".", "/")}Goal goal");
                    }

                    goalParameters.Add(lines[lineNumber]);
                }
                else if (foundDelimeters == 1)
                {
                    if (resultActionParameters.Count == 0)
                    {
                        resultActionParameters.Add("Header header");
                        resultActionParameters.Add("actionlib_msgs/GoalStatus status");
                        resultActionParameters.Add($"{Name.Replace(".", "/")}Result result");
                    } else
                    {
                        resultParameters.Add(lines[lineNumber]);
                    }
                }
                else if (foundDelimeters == 2)
                {
                    if (feedbackActionParameters.Count == 0)
                    {
                        feedbackActionParameters.Add("Header header");
                        feedbackActionParameters.Add("actionlib_msgs/GoalStatus status");
                        feedbackActionParameters.Add($"{Name.Replace(".", "/")}Feedback feedback");
                    } else
                    {
                        feedbackParameters.Add(lines[lineNumber]);
                    }
                } else
                {
                    throw new InvalidOperationException($"Action file has an unexpected amount of --- delimeters.");
                }
            }

            return (goalParameters, resultParameters, feedbackParameters, goalActionParameters, resultActionParameters,
                feedbackActionParameters);
        }
    }
}
