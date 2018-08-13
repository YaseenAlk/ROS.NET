using System;
using System.IO;

namespace YAMLParser
{
    public static class Templates
    {
        public static void LoadTemplateStrings(string templateProjectPath)
        {
            Templates.MessagesProj = File.ReadAllText(Path.Combine(templateProjectPath, "Messages._csproj"));
            Templates.MsgPlaceHolder = File.ReadAllText(Path.Combine(templateProjectPath, "PlaceHolder._cs"));
            Templates.SrvPlaceHolder = File.ReadAllText(Path.Combine(templateProjectPath, "SrvPlaceHolder._cs"));
            Templates.ActionMessagesPlaceHolder = File.ReadAllText(Path.Combine(templateProjectPath, "ActionMessagesPlaceHolder._cs"));
            Templates.ActionMessageTemplate = File.ReadAllText(Path.Combine(templateProjectPath, "ActionMessageTemplate._cs"));
            Templates.InnerMessageTemplate = File.ReadAllText(Path.Combine(templateProjectPath, "InnerMessageTemplate._cs"));

            // MessageBase files
            Templates.MessageBaseActionLibMsgsGoalID = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "actionlib_msgs", "GoalID._cs"));
            Templates.MessageBaseActionLibMsgsGoalStatus = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "actionlib_msgs", "GoalStatus._cs"));
            Templates.MessageBaseActionLibMsgsGoalStatusArray = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "actionlib_msgs", "GoalStatusArray._cs"));
            Templates.MessageBaseGeometryMsgsQuaternion = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "geometry_msgs", "Quaternion._cs"));
            Templates.MessageBaseGeometryMsgsTransform = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "geometry_msgs", "Transform._cs"));
            Templates.MessageBaseGeometryMsgsTransformStamped = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "geometry_msgs", "TransformStamped._cs"));
            Templates.MessageBaseGeometryMsgsVector3 = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "geometry_msgs", "Vector3._cs"));
            Templates.MessageBaseRosGraphMsgsClock = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "rosgraph_msgs", "Clock._cs"));
            Templates.MessageBaseRosGraphMsgsLog = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "rosgraph_msgs", "Log._cs"));
            Templates.MessageBaseStdMsgsDuration = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "std_msgs", "Duration._cs"));
            Templates.MessageBaseStdMsgsHeader = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "std_msgs", "Header._cs"));
            Templates.MessageBaseStdMsgsString = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "std_msgs", "String._cs"));
            Templates.MessageBaseStdMsgsTime = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "std_msgs", "Time._cs"));
            Templates.MessageBaseTfTfMessage = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "tf", "tfMessage._cs"));
            Templates.MessageBaseRosMessage = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "RosMessage._cs"));
            Templates.MessageBaseRosService = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "RosService._cs"));
            Templates.MessageBaseTimeData = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "TimeData._cs"));
            Templates.MessageBaseAttributes = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "Attributes._cs"));
            Templates.MessageBaseFeedbackActionMessage = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "FeedbackActionMessage._cs"));
            Templates.MessageBaseGoalActionMessage = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "GoalActionMessage._cs"));
            Templates.MessageBaseInnerActionMessage = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "InnerActionMessage._cs"));
            Templates.MessageBaseResultActionMessage = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "ResultActionMessage._cs"));
            Templates.MessageBaseWrappedFeedbackMessage = File.ReadAllText(Path.Combine(templateProjectPath, "MessageBase", "WrappedFeedbackMessage._cs"));
        }

        internal static string MessagesProj { get; set; }
        internal static string MsgPlaceHolder { get; set; }
        internal static string SrvPlaceHolder { get; set; }
        internal static string ActionPlaceHolder { get; set; }
        internal static string ActionMessagesPlaceHolder { get; set; }
        internal static string ActionMessageTemplate { get; set; }
        internal static string InnerMessageTemplate { get; set; }

        // MessageBase files
        internal static string MessageBaseActionLibMsgsGoalID { get; set; }
        internal static string MessageBaseActionLibMsgsGoalStatus { get; set; }
        internal static string MessageBaseActionLibMsgsGoalStatusArray { get; set; }
        internal static string MessageBaseGeometryMsgsQuaternion { get; set; }
        internal static string MessageBaseGeometryMsgsTransform { get; set; }
        internal static string MessageBaseGeometryMsgsTransformStamped { get; set; }
        internal static string MessageBaseGeometryMsgsVector3 { get; set; }
        internal static string MessageBaseRosGraphMsgsClock { get; set; }
        internal static string MessageBaseRosGraphMsgsLog { get; set; }
        internal static string MessageBaseStdMsgsDuration { get; set; }
        internal static string MessageBaseStdMsgsHeader { get; set; }
        internal static string MessageBaseStdMsgsString { get; set; }
        internal static string MessageBaseStdMsgsTime { get; set; }
        internal static string MessageBaseTfTfMessage { get; set; }
        internal static string MessageBaseRosMessage { get; set; }
        internal static string MessageBaseRosService { get; set; }
        internal static string MessageBaseTimeData { get; set; }
        internal static string MessageBaseAttributes { get; set; }
        internal static string MessageBaseFeedbackActionMessage { get; set; }
        internal static string MessageBaseGoalActionMessage { get; set; }
        internal static string MessageBaseInnerActionMessage { get; set; }
        internal static string MessageBaseResultActionMessage { get; set; }
        internal static string MessageBaseWrappedFeedbackMessage { get; set; }
    }
}
