﻿
#region

using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
//using Point = System.Drawing.Point;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using d = System.Drawing;
using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using PixelFormat = System.Windows.Media.PixelFormat;
//using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;
using Rectangle = System.Windows.Shapes.Rectangle;
using System.ComponentModel;
using System.Windows.Data;
using window = DREAMPioneer.SurfaceWindow1;
#endregion

namespace DREAMPioneer
{
    public partial class RobotControl : UserControl
    {
        //THIS IS TEMPORARY!!!!
        public const string combinedMapPrefix = "/robot_brain_1";
        

        private double xPos;
        private double yPos;
        public double theta;
        public double transx;
        public double transy;
        public double scalex;
        public double scaley;
        public bool sendnext;
        private Point RobotPosition;


        public static Dictionary<int, CommonList> TwoInAMillion = new Dictionary<int, CommonList>();
        public static List<CommonList> OneInAMillion = new List<CommonList>();

        public static void DoneCheck(int index)
        {
            DoneCheck(index, true);
        }
        public static void DoneCheck(int index, bool Decay)
        {            
            if (TwoInAMillion.ContainsKey(index) && TwoInAMillion[index].RobotInfowned.ContainsKey(index))
            {
                bool Done = true;
                Robot_Info RI = TwoInAMillion[index].RobotInfowned[index];
                RI.done = true;
                RI.myPoints = new List<Point>(TwoInAMillion[index].P_List);
                SurfaceWindow1.current.Dispatcher.BeginInvoke(new Action(() => RobotColor.freeMe(RI.RoboNum)));
                foreach (Robot_Info DoneCheck in TwoInAMillion[index].RoboInfo)
                    if (!DoneCheck.done)
                        Done = false;

                if (Done)
                {
                    if (Decay)
                    {
                        new Thread(Atrophy).Start(TwoInAMillion[index]);
                    }
                    else
                    {
                        SurfaceWindow1.current.Dispatcher.Invoke(new Action(() =>
                        {
                            if (TwoInAMillion.ContainsKey(index))
                               {
                                   foreach (GoalDot GD in TwoInAMillion[index].Dots)
                                       window.current.DotCanvas.Children.Remove(GD);
                                   TwoInAMillion[index].Dots.Clear();
                                   TwoInAMillion[index].P_List.Clear();
                                   TwoInAMillion[index].RobotInfowned.Clear();
                               }
                           }));
                    }
                    lock (RobotControl.OneInAMillion)
                    {
                        List<int> refs = new List<int>();
                        if (RobotControl.TwoInAMillion.ContainsKey(index))
                        {
                            foreach (KeyValuePair<int, CommonList> kvp in TwoInAMillion)
                            {
                                if (kvp.Value == RobotControl.TwoInAMillion[index])
                                    refs.Add(kvp.Key);
                            }
                            foreach (int i in refs)
                            {
                                RobotControl.TwoInAMillion.Remove(i);
                            }
                        }
                    }
                }
                else
                    Console.WriteLine("NOT DONE YET");
            }
        }


        private const int DECAY_TIME = 10;
        private const double delta = 0.01;
        private const double interdotdistance = 0.1;
        private static Action<GoalDot> tasteit = null;
        private static void Atrophy(object list)
        {
            CommonList CL = (CommonList)list;

            List<GoalDot> Copy = new List<GoalDot>(CL.Dots);

            if (tasteit == null)
                SurfaceWindow1.current.Dispatcher.BeginInvoke(new Action(() => { tasteit = new Action<GoalDot>((g) => { g.Opacity -= delta; if (g.Opacity == 0) window.current.DotCanvas.Children.Remove(g); }); }));


            CL.Dots.Clear();
            CL.P_List.Clear();
            CL.RobotInfowned.Clear();
            while (Copy.Count > 0)
            {
                double lasto = 1;
                double o = 0;
                for (int i = 0; i < Copy.Count; i++)
                {
                    if (i == 0 || lasto <= (1 - interdotdistance))
                    {
                        GoalDot GD = Copy[i];
                        lasto = o;
                        SurfaceWindow1.current.Dispatcher.Invoke(new Action(() => o = GD.Opacity));
                        if (o == 0)
                        {
                            window.current.Dispatcher.BeginInvoke(tasteit, new object[] { GD });
                        }
                        else
                        {
                            window.current.Dispatcher.BeginInvoke(tasteit, new object[] { GD });
                        }
                    }
                }
                Thread.Sleep(DECAY_TIME);
            }
        }

        public string TopicName
        {
            get
            {
                string ret = "";
                Dispatcher.Invoke(new Action(() =>
                {
                    ret = GetValue(TopicProperty) as string;
                }));
                return ret;
            }
            set
            {
                Dispatcher.Invoke(new Action(() => SetValue(TopicProperty, value)));
            }
        }


        private Thread waitforinit;
        private static NodeHandle imagehandle;

        public static readonly DependencyProperty TopicProperty = DependencyProperty.Register(
            "Topic",
            typeof(string),
            typeof(RobotControl),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.None, (obj, args) =>
                {
                    if (obj is RobotControl)
                    {
                        RobotControl target = obj as RobotControl;
                        target.TopicName = (string)args.NewValue;
                        if (!ROS.isStarted())
                        {
                            if (target.waitforinit == null)
                                target.waitforinit = new Thread(new ThreadStart(target.waitfunc));
                            if (!target.waitforinit.IsAlive)
                            {
                                target.waitforinit.Start();
                            }
                        }
                        else
                            target.SetupTopic();
                    }
                }));

        private void waitfunc()
        {
            while (!ROS.initialized || !SurfaceWindow1.current.GOGOGO)
            {
                Thread.Sleep(100);
            }
            SetupTopic();
        }
        public int ROBOT_TO_ADD;
        private void SetupTopic()
        {
            SetupTopic(ROBOT_TO_ADD);
        }


        private int MyLastCount = -9001;
        private void SetupTopic(int index)
        {
            robot.ID = index;
            ROSData myData = window.current.ROSStuffs[index];
            if (imagehandle == null)
                imagehandle = new NodeHandle();
            if (myData.robotsub != null)
                myData.robotsub.shutdown();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                robot.SetSize(10, 10);
            }));


            //done in rosdata?
            //myData.goalPub = imagehandle.advertise<gm.PoseArray>(myData.Name + "/goal_list", 1000);

#if !TRANSFORMZ
            myData.robotposesub = imagehandle.subscribe<gm.PoseWithCovarianceStamped>(myData.Name + "/amcl_pose", 1, (p) =>
                {
                    double x = p.pose.pose.position.x * (double)ROS_ImageWPF.MapControl.PPM;
                    double y = p.pose.pose.position.y * (double)ROS_ImageWPF.MapControl.PPM;
                    emQuaternion q = new emQuaternion(p.pose.pose.orientation);
                    double t = (new emMatrix3x3(q).getEuler().yaw * 180 / Math.PI) + 90.0;
                    Dispatcher.BeginInvoke(new Action(() => updatePOS(x, y, t)));
                    RobotPosition = new Point(p.pose.pose.position.x, p.pose.pose.position.y);
                    SurfaceWindow1.current.ROSStuffs[robot.ID].endCheckIn();
                });
#else
            myData.robotposesub = imagehandle.subscribe<nm.Odometry>(myData.Name + "/amcl/pose", 1, (k) =>
             {
                gm.Vector3 vec;
                gm.Quaternion quat;                
                tf_node.instance.transformFrame(myData.Name + "/odom", myData.Name+"/map", out vec, out quat);                
                double x = (vec.x) * (double)ROS_ImageWPF.MapControl.PPM;
                double y = (-vec.y) * (double)ROS_ImageWPF.MapControl.PPM;
                double t = new emMatrix3x3(new emQuaternion(quat)).getEuler(2).yaw / Math.PI;
                
                //IT TOOK ME AN HOUR TO FIND THIS FUCKING WRITELINE YOU BASTARD ZOMG
                //Console.WriteLine(t);

                //if (x != xPos || y != yPos || t != theta)
                    Dispatcher.BeginInvoke(new Action(() =>
                   {
                       updatePOS(x, y, t);
                   }));
                if (x == 0 && y == 0 && t == 0)
                    Console.WriteLine(myData.Name+" ODOM TO MAP TRANSFORM = BORKED!");
                  
            }, "*");
#endif

            WaypointHelper.PubSubs[myData.RobotNumber].SubSetup(myData.Name + "/move_base/status", (j) =>
                {
                    MyLastCount = j.status_list.Length;
                    if (TwoInAMillion.ContainsKey(index))
                    {
                        Dictionary<string, WaypointHelper> ericisthegreatest = new Dictionary<string, WaypointHelper>();

                        WaypointHelper wh = new WaypointHelper();


                        foreach (Messages.actionlib_msgs.GoalStatus g in j.status_list)
                        {
                            if (TwoInAMillion.ContainsKey(index) && TwoInAMillion[index].RobotInfowned.ContainsKey(index) && g.goal_id.id.data != TwoInAMillion[index].RobotInfowned[index].NextID)
                                continue;
                            byte status = (byte)g.status;
                            switch (status)
                            {
                                case 0:// PENDING
                                    if (AmIClose(g.goal_id.id.data) && TwoInAMillion[index].RobotInfowned[index].myList.Count > 1)
                                    {
                                        MoveOn(myData.RobotNumber, g.goal_id.id.data, ref wh);
                                        Console.WriteLine("Meh, " + myData.RobotNumber + " is close enough to " + g.goal_id.id.data);
                                    }
                                    break;
                                case 1://ACTIVE
                                    if (AmIClose(g.goal_id.id.data) && TwoInAMillion[index].RobotInfowned[index].myList.Count > 1)
                                    {   
                                        Console.WriteLine("Meh, " + myData.RobotNumber + " is close enough to " + g.goal_id.id.data);
                                        MoveOn(myData.RobotNumber, g.goal_id.id.data, ref wh);
                                    }
                                    break;
                                case 2://PREEMPTED (Terminal state)
                                    Console.WriteLine("" + myData.RobotNumber + " - wp"+g.goal_id.id.data+" - PREEMPTED (terminal)");
                                    MoveOn(myData.RobotNumber, g.goal_id.id.data, ref wh);
                                    break;
                                case 3://SUCCEEDED (Terminal state)
                                    Console.WriteLine("" + myData.RobotNumber + " - wp" + g.goal_id.id.data + " - SUCCESS (terminal)");
                                    MoveOn(myData.RobotNumber, g.goal_id.id.data, ref wh);
                                    break;
                                case 4://ABORTED (Terminal state)
                                    Console.WriteLine("" + myData.RobotNumber + " - wp" + g.goal_id.id.data + " - ABORTED (terminal)");                                    
                                    MoveOn(myData.RobotNumber, g.goal_id.id.data, ref wh);
                                    break;
                                case 5://REJECTED(Terminal state)
                                    Console.WriteLine("" + myData.RobotNumber + " - wp" + g.goal_id.id.data + " - REJECTED (terminal)");
                                    MoveOn(myData.RobotNumber, g.goal_id.id.data, ref wh);
                                    break;
                                case 6://PREEMPTING
                                    Console.WriteLine("" + myData.RobotNumber + " - wp" + g.goal_id.id.data + " - Preempting");
                                    break;
                                case 7://RECALLING
                                    Console.WriteLine("" + myData.RobotNumber + " - wp" + g.goal_id.id.data + " - Recalling");
                                    break;
                                case 8://RECALLED (Ternimal state)
                                    Console.WriteLine("" + myData.RobotNumber + " - wp" + g.goal_id.id.data + " - Recalled (terminal)");
                                    MoveOn(myData.RobotNumber, g.goal_id.id.data, ref wh);
                                    break;
                                case 9://LOST (Should not be true)
                                    Console.WriteLine("" + myData.RobotNumber + " - wp" + g.goal_id.id.data + " - LAHOOOOOZER");
                                    break;
                                default:
                                    Console.WriteLine("" + myData.RobotNumber + " - wp" + g.goal_id.id.data + " - ERROR IN SWTICH");
                                    break;

                            }

                            if (wh != null)
                            {
                                ericisthegreatest.Add(g.goal_id.id.data, wh);
                                wh.status = g.status;
                            }
                        }


                        updateGoal(myData.RobotNumber);

                    }

                });
        }
        private void MoveOn(int index, string ID, ref WaypointHelper wh)
        {
            wh = WaypointHelper.LookUp(ID);
            //WaypointHelper.Cancel(index, ID);
            if (wh != null && !TwoInAMillion[index].RobotInfowned[index].done)
            {
                TwoInAMillion[index].RobotInfowned[index].myList.Dequeue();
                TwoInAMillion[index].RobotInfowned[index].myPoints.RemoveAt(0);
                if (TwoInAMillion[index].RobotInfowned[index].myList.Count > 0)
                {
                    WaypointHelper.Publish(index, TwoInAMillion[index].RobotInfowned[index].myList.Peek());
                    return;
                }
            }
            if (RobotControl.TwoInAMillion.ContainsKey(index) && RobotControl.TwoInAMillion[index].P_List.Count == 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    RobotControl.DoneCheck(index);
                }));
                return;
            }
        }
        private bool AmIClose(string id)
        {
            if (distance(WaypointHelper.LookUp(id).realpoint, RobotPosition) < 5)
                return true;
            return false;            
        }




        public gm.Vector3 convert(gm.Quaternion q)
        {
            emQuaternion eq = new emQuaternion(q);
            gm.Vector3 ret = new gm.Vector3();
            double w2 = q.w * q.w;
            double x2 = q.x * q.x;
            double y2 = q.y * q.y;
            double z2 = q.z * q.z;
            double unitLength = eq.length();    // Normalized == 1, otherwise correction divisor.
            double abcd = q.w * q.x + q.y * q.z;
            double eps = Math.E;
            double pi = Math.PI;
            if (abcd > (0.5 - eps) * unitLength)
            {
                ret.z = 2 * Math.Atan2(q.y, q.w);
                ret.y = pi;
                ret.x = 0;
            }
            else if (abcd < (-0.5 + eps) * unitLength)
            {
                ret.z = -2 * Math.Atan2(q.y, q.w);
                ret.y = -pi;
                ret.x = 0;
            }
            else
            {
                double adbc = q.w * q.z - q.x * q.y;
                double acbd = q.w * q.y - q.x * q.z;
                ret.z = Math.Atan2(2 * adbc, 1 - 2 * (z2 + x2));
                ret.y = Math.Asin(2 * abcd / unitLength);
                ret.x = Math.Atan2(2 * acbd, 1 - 2 * (y2 + x2));
            }

            return ret;

        }

        private gm.Pose[] MakePoseArray(List<Point> PList)
        {
            gm.Pose[] ret = new gm.Pose[PList.Count];
            for (int i = 0; i < PList.Count; i++)
                ret[i] = new Messages.geometry_msgs.Pose { position = new Messages.geometry_msgs.Point { x = PList[i].X * (double)ROS_ImageWPF.MapControl.MPP, y = PList[i].Y * (double)ROS_ImageWPF.MapControl.MPP, z = 0 }, orientation = new Messages.geometry_msgs.Quaternion { w = 0, x = 0, y = 0, z = 0 } };

            return ret;
        }


        public void updateWaypoints(double x, double y, double xx, double yy)
        {
            transx = x;
            transy = y;
            scalex = xx;
            scaley = yy;
            sendnext = true;
        }

        public void updatePOS(double x, double y, double t)
        {
            xPos = x;
            yPos = y;
            Canvas.SetLeft(robot, xPos - robot.Width / 2);

            Canvas.SetTop(robot, (yPos - robot.Height / 2));
            robot.SetSize(10, 10);
            robot.Theta = t;
            theta = t;
        }

        private bool compare(Point pos, Point waypoint)
        {
            if (distance(pos, waypoint) < 40)
                return true;
            else return false;
        }
        public double distanceTHATISNTWRONG(Point q, Point p)
        {
            return distance(q.X, q.Y, p.X, p.Y);
        }
        public double distance(Point q, Point p)
        {
            return distance(q.X + transx, q.Y + transy, p.X, p.Y);
        }

        public double distance(double x2, double y2, double x1, double y1)
        {
            return Math.Sqrt(
                (x2 - x1) * (x2 - x1)
                + (y2 - y1) * (y2 - y1));
        }


        public void SetOpacity(Double opa)
        {
            robot.Dot.Opacity = opa;

        }

        private void updateGoal(int index)
        {
            //now calls SetGoal, jumping over CheckUnique
            window.current.SetGoal(index);

            //CheckUnique(index);
        }



        public RobotControl(int R)
        {
            ROBOT_TO_ADD = R;
            InitializeComponent();
        }


        /*******************************************************************************/
        /*  Depricated since we now take not of shared lists when we send lists out
 
            public void CheckUnique(List<Point> P_List, int R)
                {
                    if (P_List.Count == 0)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            RobotControl.DoneCheck(R);
                        }));
                        return;
                    }

                    Brush MyColor = RobotColor.getMyColor(R);
                    SurfaceWindow1.current.Dispatcher.BeginInvoke(new Action(() =>
                         {
                             window.current.ROSStuffs[R].myRobot.robot.setArrowColor(MyColor);
                         }));
           
                    CommonList DisList = null;
           
                    if (OneInAMillion.Count == 0)
                    {
                        //If there are no saved lists it is unique

                        DisList = new CommonList(P_List, R, MyColor, 1); 
                        OneInAMillion.Add(DisList);
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            window.current.AddGoalDots(P_List, DisList.Dots, MyColor);
                            if (DisList.Dots.Count >= 1)
                                DisList.Dots[0].NextOne = true;
                            foreach (Robot_Info RI in DisList.RoboInfo)
                                if (RI.RoboNum == R)
                                    window.current.SetGoal(R, P_List, DisList, RI);
                        }));
                        return;
                    }

                    foreach (CommonList CL in OneInAMillion)
                    {

                        //It's not unique beacause a path already exists for this robot.

                        foreach (Robot_Info RI in CL.RoboInfo)
                            if (RI.RoboNum == R && !RI.done)
                            {
                                Dispatcher.BeginInvoke(new Action<int, List<Point>, CommonList, Robot_Info>(window.current.SetGoal), new object[] { R, P_List, CL, RI });
                                return;
                            }

                        int j;

                        if (P_List.Count <= CL.P_List.Count)
                        {
                            j = CL.P_List.Count - P_List.Count;
                            for (int i = P_List.Count - 1; i >= 0; i--)
                            {
                                if (i == 0)
                                    if (P_List[i] == CL.P_List[i + j])
                                    {

                                        //NOT UNIQUE Shorter or Equal To



                                        CL.RoboInfo.Add(new Robot_Info(R, P_List.Count, MyColor, CL.RoboInfo[CL.RoboInfo.Count - 1].Position + 1));
                                        foreach (Robot_Info RI in CL.RoboInfo)
                                            if (RI.RoboNum == R)
                                            {
                                                Dispatcher.BeginInvoke(new Action<int, List<Point>, CommonList, Robot_Info>(window.current.SetGoal), new object[] { R, P_List, CL, RI });
                                                return;
                                            }
                                    }

                                    else if (P_List[i] != CL.P_List[i + j]) break;
                            }
                        }
                        else
                        {
                            j = P_List.Count - CL.P_List.Count;
                            for (int i = CL.P_List.Count - 1; i >= 0; i--)
                            {
                                if (i == 0)
                                    if (P_List[i + j] == CL.P_List[i])
                                    {
                                        //NOT UNIQUE Longer

                                        Dispatcher.BeginInvoke(new Action(()=>
                                        CL.RoboInfo.Add(new Robot_Info(R, P_List.Count, MyColor, CL.RoboInfo[CL.RoboInfo.Count - 1].Position + 1))));
                                       //List<Point> Better_List = new List<Point>(P_List.Except<Point>(CL.P_List));

                                        CL.P_List.Clear();
                                        CL.P_List = P_List;
                                        // window.current.AddGoalDots(Better_List, CL.Dots, MyColor);
                                        foreach (Robot_Info RI in CL.RoboInfo)
                                            if (RI.RoboNum == R)
                                            {
                                                Dispatcher.BeginInvoke(new Action<int, List<Point>, CommonList, Robot_Info>(window.current.SetGoal), new object[] { R, P_List, CL, RI });
                                                return;
                                            }
                                    }
                                    else if (P_List[i + j] != CL.P_List[i]) break;
                            }
                        }
                    }
                    //IT IS UNIQUE


                   DisList = new CommonList(P_List, R, MyColor, 1); 
                    OneInAMillion.Add(DisList);

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        window.current.AddGoalDots(P_List, DisList.Dots, MyColor);
                        if (DisList.Dots.Count >= 1)
                            DisList.Dots[0].NextOne = true;
                        foreach (Robot_Info RI in DisList.RoboInfo)
                            if (RI.RoboNum == R)
                                window.current.SetGoal(R, P_List, DisList, RI);
                    }));
                    return;
                }*/
        /**************************************************************************/


        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string strPropertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(strPropertyName));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //image.Transform = new ScaleTransform(1, -1, ActualWidth / 2, ActualHeight / 2);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //image.Transform = new ScaleTransform(1, -1, ActualWidth / 2, ActualHeight / 2);
        }
        #endregion

    }
}