/*==========  OpenTK 低版本模板_动画 ===========
 * 1,内置立体件
 * 2,空间圆,三点画圆弧,通过三点找圆心,空间圆柱,圆台，圆锥
 * 3,三维曲面体
 * 4,鼠标与键盘(拍照与退出)操控
 * 
==========必须带的三个文件[添加现有项]=========:
Axles3D.cs
Axles3D.designer.cs
Axles3D.resx
 * 
=========使用TK必须引用的库[添加引用]========:
bin\Debug\ArcPlotClass.dll
bin\Debug\OpenTK.GLControl.dll
bin\Debug\OpenTK.Compatibility.dll
bin\Debug\OpenTK.GLControl.dll
 *  
 * 5,将控件Axles3D加入自己窗体
 * 6,Axles3D.cs //TK控件,写算法与显示函数
        ====== By daode1212  2016-02-17
 *
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;  // DllImport 必需
using ArcPlotClass ;//通过空间三点画圆弧必需
using OpenTK; //
using OpenTK.Graphics;//


namespace AxlesGL
{
    struct P3D  //自定义数据结构
    {
        public double x { set; get; }
        public double y { set; get; }
        public double z { set; get; }
    }
    
    public partial class Axles3D : GLControl
    {
        
        #region 一些基本的定义
        bool _loaded = false;
        double _azi, _ele;
        double _angleX, _angleY, _angleZ;

        //旋转角度:
        float theta = 0.0f;

        //旋转轴:
        float[] axis = { 1.0f, 0.0f, 0.0f };

        //鼠标上次和当前坐标（映射到单位半球面）:
        float[] lastPos = { 0.0f, 0.0f, 0.0f };
        float[] curPos = { 0.0f, 0.0f, 0.0f };

        //上一次转换矩阵:
        float[] lastMatrix =
		{
			1.0f, 0.0f, 0.0f, 0.0f,
			0.0f, 1.0f, 0.0f, 0.0f,
			0.0f, 0.0f, 1.0f, 0.0f,
			0.0f, 0.0f, 0.0f, 1.0f
		};

        //点集定义:
        P3D[] pt = new P3D[9999];
        int pc = 0;

        //缩放倍数:
        double whl = 0;

        //移动初始值:
        double x0, y0, z0;     
        
        //移动偏移量:        
        double dltx0, dlty0, dltz0;
        double dltx, dlty, dltz;

        double ng,mg,mg2,mg3, lg;//动画控制
        double merSpeed, venSpeed, earSpeed, marSpeed, jupSpeed, satSpeed, uraSpeed, nepSpeed;
        public static double planetRotateSpeed;
        bool showOrbit;



        //方向角(XOY平面旋转,转轴:Z轴):
        public double Azimuth
        {
            get
            {
                return _azi;
            }
            set
            {
                _azi = value;
                if (_loaded)
                    DrawAll();
            }
        }

        //仰俯角:
        public double Elevation
        {
            get
            {
                return _ele;
            }
            set
            {
                _ele = value;
                if (_loaded)
                    DrawAll();
            }
        }

        //转角,X:
        public double AngleX
        {
            get
            {
                return _angleX;
            }
            set
            {
                _angleX = value;
                if (_loaded)
                    DrawAll();
            }
        }

        //转角,Y:
        public double AngleY
        {
            get
            {
                return _angleY;
            }
            set
            {
                _angleY = value;
                if (_loaded)
                    DrawAll();
            }
        }

        //转角,Z:
        public double AngleZ
        {
            get
            {
                return _angleZ;
            }
            set
            {
                _angleZ = value;
                if (_loaded)
                    DrawAll();
            }
        }
        #endregion
        
        //画出全部图像:
        private void DrawAll()
        {
            #region  SET GL(设置GL)
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.ShadeModel(ShadingModel.Smooth);
            GL.ClearColor(Color.Black);
           // GL.ClearColor(0.1f, 0.2f, 0.2f, 0.7f); //画布背景色
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            //SETVIEW(设置视口):
            GL.Viewport(0, 0, this.Bounds.Width, this.Bounds.Height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            Glu.Perspective(45.0f, (float)this.Bounds.Width / (float)this.Bounds.Height, 1.0, 10.0);
            Glu.LookAt(0.0, 0.0, 4.0, 0.0, 0.0, 3.0, 0.0, 1.0, 0.0);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            #endregion

            //画大地,画天空(静止背景)=================================
            //DrawBg();  //Draw the Background
            //=========================================================

            #region 全局变换(缩放,平移,旋转)
            //全局缩放:
            GL.Scale(1 + whl, 1 + whl, 1 + whl);
            whl = 0;

            //全局平移(对象的X,Y轴方向平移):
            GL.Translate(dltx, dlty, dltz);
            //dltx = 0; dlty = 0; dltz = 0;

            //全局旋转,计算新的旋转矩阵，即：M = E · R = R
             GL.Rotate(theta, axis[0], axis[1], axis[2]);
            //GL.Rotate(theta,0, 0, 1);
            //左乘上前一次的矩阵，即：M = R · L
            GL.MultMatrix(lastMatrix);

            //保存此次处理结果，即：L = M
            GL.GetFloat(GetPName.ModelviewMatrix, lastMatrix);
            theta = 0.0f;

            
            #endregion


            //================  动画  ===================
            this.Invalidate(); //引发重绘，动画必须 
            ng = 0.1;  //速度控制          
            theta = (float)(ng); //在原基础上，递增的旋转角
            mg =mg % 360 + 0.01;
            mg2 = mg2 % 360 + 0.05;
            mg3 = mg3 % (2*Math.PI) + 0.01;

            planetRotateSpeed = 20;
            int zoomscale = 60 ;     //画行星和运行轨迹，把星体半径放大n倍，否则太小
            showOrbit = false;
            merSpeed = merSpeed % 360 + getSpeed(88);
            venSpeed = venSpeed % 360 + getSpeed(225);
            earSpeed = earSpeed % 360 + getSpeed(365);
            marSpeed = marSpeed % 360 + getSpeed(657);
            jupSpeed = jupSpeed % 360 + getSpeed(12*365);
            satSpeed = satSpeed % 360 + getSpeed(29*365);
            uraSpeed = uraSpeed % 360 + getSpeed(84*365);
            nepSpeed = nepSpeed % 360 + getSpeed(165*365);


            //也可结合时间控制：DateTime.Now.Ticks,DateTime.Now.Millisecond
            //======================================



            //DrawAixs();//绘制坐标轴            
            //DrawChar();//绘制字母X,Y,Z


            //DrawModels();//内置立体件
            //DrawTorus(); //圆环轮胎体
            //DrawPlane();//绘制X,Y,Z坐标平面

            //DrawArc();//通过不在同一直线上的三点画圆弧
            //DrawCylinder();//绘制空间圆柱,圆台，圆锥


            // DrawBody(0,0,0,43,430,0,0,1,mg, Color.OrangeRed);//SUN
           // GenerateStars(10);
            DrawPlanet(0, 43* zoomscale, nepSpeed, false, Color.OrangeRed, 0);//SUN
            DrawPlanet(3590,0.15*zoomscale, merSpeed, false,Color.Silver,0); //Mercury
            DrawPlanet(6720,0.37* zoomscale, venSpeed, false,Color.Gold,0); //Venus
            DrawPlanet(9300, 0.39* zoomscale, earSpeed, true,Color.DeepSkyBlue,1);//Earth
            DrawPlanet(14160, 0.21* zoomscale, marSpeed, false,Color.DarkRed,0);//Mars
            DrawPlanet(48380, 4.34* zoomscale, jupSpeed, true,Color.Brown,4);//Jupiter
            DrawPlanet(89070, 3.62* zoomscale, satSpeed, true,Color.SandyBrown,4);//Saturn
            DrawPlanet(178400, 1.576* zoomscale, uraSpeed, false,Color.DeepSkyBlue,0);//Uranus
            DrawPlanet(279300, 1.024* zoomscale, nepSpeed, false,Color.DeepSkyBlue,0);//Neptune
            //======================================
            GL.Disable(EnableCap.DepthTest);
            SwapBuffers();
        }       
        
        #region 鼠标与键盘事件

        //鼠标移动_旋转对象:
        void Motion(int x, int y)
        {
            float d, dx, dy, dz;

            // 计算当前的鼠标单位半球面(Hemishere)坐标
            if (!Hemishere(x, y, GetSquareLength(), curPos))
            {
                return;
            }

            // 计算移动量的三个方向分量
            dx = curPos[0] - lastPos[0];
            dy = curPos[1] - lastPos[1];
            dz = curPos[2] - lastPos[2];

            //如果有移动:
            if ((0.0f != dx) || (0.0f != dy) || (0.0f != dz))
            {

                //计算移动距离，用来近似移动的球面距离
                d = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

                //通过移动距离计算移动的角度
                theta = (float)d * 180.0f;

                //计算移动平面的法向量，即：lastPos × curPos (叉乘,外积)
                axis[0] = lastPos[1] * curPos[2] - lastPos[2] * curPos[1];
                axis[1] = lastPos[2] * curPos[0] - lastPos[0] * curPos[2];
                axis[2] = lastPos[0] * curPos[1] - lastPos[1] * curPos[0];

                //记录当前的鼠标单位半球面坐标:
                lastPos[0] = curPos[0];
                lastPos[1] = curPos[1];
                lastPos[2] = curPos[2];

                this.Parent.Text = "Xpos=" + lastPos[0].ToString("0.00") + ",Ypos=" + lastPos[1].ToString("0.00") + ",Zpos=" + lastPos[2].ToString("0.00");
            }
        }  

        //鼠标弹开事件:
        private void Axles3D_MouseUp(object sender, MouseEventArgs e)
        {
            dltx = 0; dlty = 0;dltz = 0;
        }

        //鼠标按下事件:
        private void Axles3D_MouseDown(object sender, MouseEventArgs e)
        {
            Hemishere(e.X, e.Y, GetSquareLength(), curPos);
            lastPos[0] = curPos[0];
            lastPos[1] = curPos[1];
            lastPos[2] = curPos[2];
            dltx0 = e.X; dlty0 = e.Y;
        }

        //鼠标移动(及按下)事件:
        private void Axles3D_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseButtons.Left == e.Button)
            {   
                //X,Y方向平移:
                Hemishere(e.X, e.Y, GetSquareLength(), curPos);
                dltx =(e.X- dltx0)/200f ;
                dlty =-(e.Y - dlty0)/200f;
                dltz = 0;                
            }
            if (MouseButtons.Right == e.Button)
            {   
                //旋转:
                Motion(e.X, e.Y);                
            }
            if (MouseButtons.Middle == e.Button)
            {
                //Z方向平移:
                Hemishere(e.X, e.Y, GetSquareLength(), curPos);
                double xm = (e.X - dltx0) / 20f;
                double ym = (e.Y - dlty0) / 20f;
                dltz = (Math.Abs(ym) > Math.Abs(xm) ? ym : xm) ;                
            }
            DrawAll();
            dltx0 = e.X; dlty0 = e.Y; dltz0 = dltz;
        }

        //鼠标滚轮事件:
        private void axles3D1_MouseWheel(object sender, MouseEventArgs e)
        {
            //缩放对象:
            if (e.Delta < 0)
            {
                whl += 0.1;
                //Azimuth += .05;//XOY平面旋转,转轴:Z轴
                //Elevation += 0.05;//转轴:Y轴
            }
            else
            {
                whl -=0.1;
                //Azimuth -= .05;//XOY平面旋转,转轴:Z轴
                //Elevation += 0.05;//转轴:Y轴
            } 
            DrawAll();            
        }

        //单击键盘事件:
        private void Axles3D_KeyPress(object sender,  System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)27)
            {
                //MessageBox.Show("EXIT Application");
                Application.Exit();
            }

            //============截屏保存图片==============
            //当 Form1_KeyDown(object sender, KeyEventArgs e)
            //用 (e.KeyCode == Keys.Enter) 
            if (e.KeyChar == (char)13)  
            {
            //    Rectangle rectOld = Rectangle.Empty;
            //    Int32 hwnd = 0;
            //    hwnd = FindWindow("Form1", null);//获取任务栏的句柄
            //    ShowWindow(hwnd, SW_HIDE);//隐藏任务栏
            //    SystemParametersInfo(SPI_GETWORKAREA, 0, ref rectOld, SPIF_UPDATEINIFILE);//屏幕范围
            //    Rectangle rectFull = Screen.PrimaryScreen.Bounds;//全屏范围
            //    SystemParametersInfo(SPI_SETWORKAREA, 0, ref rectFull, SPIF_UPDATEINIFILE);//窗体全屏幕显示
               prtScn(); //截屏保存图片
            //    ShowWindow(hwnd, SW_SHOW);//隐藏任务栏
            //    SystemParametersInfo(SPI_SETWORKAREA, 0, ref rectOld, SPIF_UPDATEINIFILE);//窗体还原
            }
        }

        #endregion

        #region  各种绘制函数

        //通过不在同一直线上的三点画圆弧:
        private void DrawArc()
        {
            ArcPlotAchieve arc = new ArcPlotAchieve();
            arc.SetDirection(true);
            arc.SetPoint(-1.2f, 0, 0, 0, 2f, 0, 1.2f, 0, 0);
            arc.SetSegment(20);
            string ss = arc.GetResult();
            //MessageBox.Show( ss);  //x,y,z;...
            string[] a1 = ss.Split(';');

            GL.Begin(BeginMode.LineStrip);
            for (int i = 0; i < a1.Length - 1; i++)
            {
                string[] p = a1[i].Split(',');
                double xx = Double.Parse(p[0]);
                double yy = Double.Parse(p[1]);
                double zz = Double.Parse(p[2]);
                GL.Vertex3(xx, yy, zz);
            }
            GL.End();
        }

        //绘制(静止背景)
        private void DrawBg()
        {
            GL.Begin(BeginMode.Quads); //画天空(静止背景)
            GL.Color3(0.0f, 0.7f, 0.2f); GL.Vertex3(-6, -4, -2);
            GL.Color3(0.6f, 0.8f, 0.2f); GL.Vertex3(6, -4, -2);
            GL.Color3(0.6f, 0.0f, 0.6f); GL.Vertex3(6, 4, -2);
            GL.Color3(0.5f, 0.0f, 0.3f); GL.Vertex3(-6, 4, -2);
            GL.End();

            GL.Begin(BeginMode.Quads); //画大地(静止背景)
            GL.Color3(0.6f, 0.0f, 0.6f); GL.Vertex3(0.25, -2, -10); //上
            GL.Color3(0.5f, 0.0f, 0.3f); GL.Vertex3(-0.25, -2, -10);//上
            GL.Color3(0.0f, 0.7f, 0.2f); GL.Vertex3(-2, -1, 2); //下
            GL.Color3(0.6f, 0.8f, 0.2f); GL.Vertex3(2, -1, 2);  //下
            GL.End();
        }

        //绘制坐标轴:
        private void DrawAixs()
        {
            //画坐标轴:
            DrawOLineInGL(0, 0, 1.1f, 2.0f, Color.Red, true);
            DrawOLineInGL(Math.PI, 0, 1.1f, 2.0f, Color.Red, false);
            DrawOLineInGL(Math.PI / 2, 0, 1.1f, 2.0f, Color.Lime, true);
            DrawOLineInGL(-Math.PI / 2, 0, 1.1f, 2.0f, Color.Lime, false);
            DrawOLineInGL(0, Math.PI / 2, 1.1f, 2.0f, Color.LightBlue, true);
            DrawOLineInGL(0, -Math.PI / 2, 1.1f, 2.0f, Color.LightBlue, false);
            //画独立长箭头:
            //DrawOLineInGL(_azi, _ele, 1.2f, 3.0f, Color.White, true);
        }

        //绘制平面:
        private void DrawPlane()
        {
            //画各正方形:
            GL.Begin(BeginMode.Quads);
            //YOZ平面,RED:
            GL.Color4(1.0f, 0.0f, 0.0f, 0.4f);
            GL.Vertex3(0.0f, 1.0f, 1.0f);
            GL.Vertex3(0.0f, 1.0f, -1.0f);
            GL.Vertex3(0.0f, -1.0f, -1.0f);
            GL.Vertex3(0.0f, -1.0f, 1.0f);
            //XOZ平面,GREEN:
            GL.Color4(0.0f, 1.0f, 0.0f, 0.4f);
            GL.Vertex3(1.0f, 0.0f, 1.0f);
            GL.Vertex3(1.0f, 0.0f, -1.0f);
            GL.Vertex3(-1.0f, 0.0f, -1.0f);
            GL.Vertex3(-1.0f, 0.0f, 1.0f);
            //XOY平面,BLUE:
            GL.Color4(0.0f, 0.0f, 1.0f, 0.4f);
            GL.Vertex3(1.0f, 1.0f, 0.0f);
            GL.Vertex3(1.0f, -1.0f, 0.0f);
            GL.Vertex3(-1.0f, -1.0f, 0.0f);
            GL.Vertex3(-1.0f, 1.0f, 0.0f);
            GL.End();
        }

        //绘制字母:
        private void DrawChar()
        {
            //以点阵方式绘制X,Y,Z三字母:
            byte[] lettersX =
            {
                0x00,0xef,0x46,0x2c,0x2c,0x18,0x18,0x18,0x34,0x34,0x62,0xf7,0x00
            };
            byte[] lettersY =
            {
                0x00,0x3c,0x18,0x18,0x18,0x18,0x18,0x2c,0x2c,0x46,0x46,0xef,0x00
            };
            byte[] lettersZ =
            {
                0x00,0xfe,0x63,0x63,0x30,0x30,0x18,0x0c,0x0c,0x06,0xc6,0x7f,0x00
            };
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1.0f);
            GL.Color3(1.0f, 0.0f, 0.0f);
            GL.RasterPos3(1.4f, 0.0f, 0.0f);
            GL.Bitmap(8, 13, 0.0f, 0.0f, 20.0f, 0.0f, lettersX);
            GL.Color3(0.0f, 1.0f, 0.0f);
            GL.RasterPos3(0.0f, 1.4f, 0.0f);
            GL.Bitmap(8, 13, 0.0f, 0.0f, 20.0f, 0.0f, lettersY);
            GL.Color3(0.3f, 0.7f, 1.0f);
            GL.RasterPos3(0.0f, 0.0f, 1.4f);
            GL.Bitmap(8, 13, 0.0f, 0.0f, 20.0f, 0.0f, lettersZ);
        }

        //绘制空间圆柱:
        private void DwCylinder(P3D v1,P3D v2,double r)
        {
            //计算方向矢量N(A,B,C):
            //P3D v1 = new P3D(); P3D v2 = new P3D(); P3D v = new P3D();
            //double r = 0.5; 
            //v1.x = 0; v1.y = -1; v1.z = 0;
            //v2.x = 1; v2.y = 1; v2.z = 1;
            double A, B, C;
            A = v1.x - v2.x; B = v1.y - v2.y; C = v1.z - v2.z;

            //计算方向矢量之模(请确保p,m不等于0):
            double p = Math.Sqrt(A * A + B * B);
            double m = Math.Sqrt(A * A + B * B + C * C);

            //计算向量之方向余弦:
            //ca=Cos(a);cb=Cos(b);cc=Cos(c)
            double ca = A / m; double cb = B / m; double cc = C / m;

            //绘制空间圆柱-2:
            GL.Begin(BeginMode.Quads); //画方块
            int[] ar = new int[4] { 0, 1, 1, 0 };
            int[] br = new int[4] { 0, 0, 1, 1 };
            double ds = m/2; double dt = 0.35;
            for (double s = 0; s < m; s += ds)
            {
                for (double t = 0; t < 2 * Math.PI; t += dt)
                {
                    double[] x = new double[4];
                    double[] y = new double[4];
                    double[] z = new double[4];
                    for (int k = 0; k < 4; k++)
                    {
                        double sv = s + ar[k] * ds;
                        double tv = t + br[k] * dt;

                        double st = Math.Sin(tv);
                        double ct = Math.Cos(tv);

                        x[k] = v1.x - sv * ca + r * (st * A * C / (m * p) + ct * B / p);
                        y[k] = v1.y - sv * cb + r * (st * B * C / (m * p) - ct * A / p);
                        z[k] = v1.z - sv * cc - r * (st * p / m);

                        GL.Color3(Math.Sin(sv-.6), Math.Cos(sv * tv), Math.Cos(.4+tv));
                        GL.Vertex3(x[k], y[k], z[k]);
                    }
                }
            }
            GL.End();
            //=========================================
        }

        //绘制空间圆柱集:
        private void DrawCylinder()
        {
            double PI = 3.1416; double stp = 0.1;


            //生成点集:
            for (double a = -PI; a < PI; a += stp)
            {
                pt[pc].x = Math.Cos(a)/2f;
                pt[pc].y = Math.Sin(a)/2f;
                pt[pc].z = Math.Cos(a)*Math.Sin(a)/2f;//a/5f;
                if (pc < 200) pc++;
            }

            //同点集生成空间圆柱:
            for (int k = 1; k < pc; k++)
            {
                DwCylinder(pt[k - 1], pt[k], (1+Math.Sin(1+k/10f)) / 15f);
            }

        }

        private void DrawPlanet(double r,double radius,double angSpeed,bool hasMoon, Color color,int moonNo)
        {
            double anger = 0;
         
            DrawBody(r * Math.Cos(anger), r * Math.Sin(anger), 0, radius, (Convert.ToInt32(radius)*10+1), 0,0,1, angSpeed, color);
            if (hasMoon) {
                for (int i = 1; i <= moonNo; i++) { 
                DrawBody((r * Math.Cos(anger) + (i-i/(i+1)+1)* radius * Math.Cos(angSpeed/4)), (r * Math.Sin(anger) + (i - i / (i+1)+1) * radius * Math.Sin(angSpeed/4)), 0, 0.1 * radius, (Convert.ToInt32(0.2*radius) * 10 + 1), 0, 0, 1, angSpeed, Color.White);
                if(showOrbit)
                    DrawMoonOrbit(r * Math.Cos(anger), r * Math.Sin(anger), (i - i / (i + 1)+1) * radius, 0, 0, 1, angSpeed);
                }
            }
                
            if(showOrbit)  
                DrawOrbit(r);
        }





        //绘制曲面体:
        private void DrawBody(double x, double y, double z, double r, int density, double vx, double vy, double vz,double anger,Color color)
        {
            double PI = 3.1416; double stp = PI/20;
            GL.PushMatrix();
            GL.LineWidth(1);
            
            GL.PointSize(4);//构成星体的一个点表示4万英里

            //GL.Translate(dltx, dlty, dltz); //三维移动,调整旋转中心,放到透视线的当中
            GL.Rotate(anger, vx, vy, vz);

            int[] ar = new int[4] { 0, 1, 1, 0 };
            int[] br = new int[4] { 0, 0, 1, 1 };

            double[] cx = new double[4];
            double[] cy = new double[4];
            double[] cz = new double[4];

            for (double b = -PI; b < PI; b += stp)
            {
                for (double a = 0; a < PI; a += stp)
                {
                    GL.Begin(BeginMode.Polygon);//画四边形
                    for (int k = 0; k < 4; k++)
                    {
                        double av = a + ar[k] * stp;
                        double bv = b + br[k] * stp;
                        GL.Color3(color);
                        //GL.Color3(1f, 0 ,0);
                        //====================算法构建区:起=======================

                        //cx[k] = 0.25 * Math.Cos(av) * (Math.Cos(bv) + mg);
                        //cy[k] = 0.25 * Math.Sin(av) * (Math.Cos(bv) + mg);
                        //cz[k] = 0.25 * Math.Sin(bv);
                        cx[k] =  r*Math.Cos(av) * Math.Cos(bv) ;
                        cy[k] =  r*Math.Sin(av) * Math.Cos(bv) ;
                        cz[k] =  r*Math.Sin(bv);

                        //=====================算法构建区:止=======================

                         GL.Vertex3(x+cx[k], y+cy[k], z+cz[k]);
                    }

                   
                   
                    GL.End();

                    ////画线 :
                  //  GL.Color3(1 - (a + b + 6) / 12, (a + PI) / 6, (b + PI) / 6);
                    GL.Begin(BeginMode.LineStrip);
                    for (int k = 0; k < 4; k++)
                    {
                        GL.Vertex3(cx[k], cy[k], cz[k]);
                    }
                    GL.End();

                    ////画点 :
                   // GL.Color3((a + PI) / 6, (b + PI) / 6, 1 - (a + b + 6) / 12);
                    GL.Begin(BeginMode.Points);
                    for (int k = 0; k < 4; k++)
                    {
                        GL.Vertex3(cx[k], cy[k], cz[k]);
                    }
                    GL.End();
                }
            }
           
            GL.PopMatrix();
           

        }

        //画多个内置立体件:
        private void DrawModels() 
        {
            float width = 3;
            float height = 3;
            float length = 3;

            GL.LineWidth(4);
            GL.PointSize(8);   
            var obj = Glu.NewQuadric();
            //Glu.QuadricDrawStyle(obj, QuadricDrawStyle.Line);
            //Glu.QuadricDrawStyle(obj, QuadricDrawStyle.Silhouette);
            //Glu.QuadricDrawStyle(obj, QuadricDrawStyle.Point);
            //Glu.QuadricDrawStyle(obj, QuadricDrawStyle.Fill); 
            Glu.QuadricDrawStyle(obj, QuadricDrawStyle.Line);

            ////画大正方形上可旋转各对象 Draw plane that the objects rest on
            //GL.PushMatrix();
            //GL.Color4(0.5f,0.3f, 0.5f, 0.5f); // Blue
            //GL.Normal3(0.0f, 1.0f, 0.0f);
            //GL.Begin(BeginMode.Quads);
            //GL.Vertex3(-10.0f, -2.5f, -10.0f);
            //GL.Vertex3(-10.0f, -2.5f, 10.0f);
            //GL.Vertex3(10.0f, -2.5f, 10.0f);
            //GL.Vertex3(10.0f, -2.5f, -10.0f);
            //GL.End();
            //GL.PopMatrix();

            ////画网线空心球
            //GL.PushMatrix();
            //GL.Color3(0.6f, 0.6f, 0.8f);
            //GL.Translate(0.0f, -3.0f, 0.0f);       
            //Glu.Sphere(obj, 2, 6, 4);//球体-----------------    
            //GL.PopMatrix();

            ////画立方体(6个正方形) Draw red cube            
            //GL.PushMatrix();
            //GL.Color3(0.5f, 0.75f, 0.5f);
            //GL.Translate(0.0f, 3.0f, 0.0f);
            //GL.Begin(BeginMode.Quads);
            ////
            //GL.Normal3(0.0f, 0.0f, -1.0f);
            //GL.Vertex3(-width, height, -length); //0
            //GL.Vertex3(width, height, -length);  //1
            //GL.Vertex3(width, -height, -length);//2
            //GL.Vertex3(-width, -height, -length); //3
            ////
            //GL.Normal3(0.0f, 0.0f, 1.0f);
            //GL.Vertex3(-width, height, length);  //4
            //GL.Vertex3(-width, -height, length); //5
            //GL.Vertex3(width, -height, length);   //6
            //GL.Vertex3(width, height, length);  //7
            ////
            //GL.Normal3(0.0f, 1.0f, 0.0f);
            //GL.Vertex3(-width, height, length);  //8 (4)
            //GL.Vertex3(width, height, length);   //9 (6)
            //GL.Vertex3(width, height, -length); //10 (0)
            //GL.Vertex3(-width, height, -length);  //11 (1)
            ////
            //GL.Normal3(0.0f, -1.0f, 0.0f);
            //GL.Vertex3(-width, -height, length); //12 (5)
            //GL.Vertex3(-width, -height, -length);//13 (2)
            //GL.Vertex3(width, -height, -length);  //14 (7)
            //GL.Vertex3(width, -height, length); //15 (3)
            ////
            //GL.Normal3(1.0f, 0.0f, 0.0f);
            //GL.Vertex3(width, height, -length);  //16 (1)
            //GL.Vertex3(width, height, length);   //17 (6)
            //GL.Vertex3(width, -height, length); //18 (3)
            //GL.Vertex3(width, -height, -length);  //19 (7)
            ////
            //GL.Normal3(-1.0f, 0.0f, 0.0f);
            //GL.Vertex3(-width, height, -length); //20 (0)
            //GL.Vertex3(-width, -height, -length);//21 (2)
            //GL.Vertex3(-width, -height, length);  //22 (4)
            //GL.Vertex3(-width, height, length); //23 (5)
            ////
            //GL.End();
            //GL.PopMatrix();

            //绿色球体 Draw green sphere            
            GL.PushMatrix();
            GL.Color3(0.0f, 1.0f, 0.0f);
            GL.Translate(-6.0f, 0.0f, 0.0f);
            Glu.Sphere(obj, 2.5f, 8, 12);
            GL.PopMatrix();

            ////黄色锥体 Draw yellow cone            
            //GL.PushMatrix();
            //GL.Color3(1.0f, 1.0f, 0.0f);
            //GL.Rotate(-90.0f, 1.0f, 0.0f, 0.0f);
            //GL.Translate(6.0f, 0.0f, -2.4f);
            //Glu.Cylinder(obj, 2.5f, 1.0f, 5.0f, 8, 12);
            //GL.PopMatrix();

            ////绘制实心圆环 Draw magenta torus            
            //GL.PushMatrix();
            //GL.Color3(1.0f, 0.0f, 1.0f);
            //GL.Translate(0.0f, 0.0f, 2.0f);
            //DrawTorus(5.0f, 1.0f, 20, 20);//绘制实心圆环
            //GL.PopMatrix();

            ////绘制八面体 Draw cyan octahedron            
            //GL.PushMatrix();
            //GL.Color3(0.0f, 1.0f, 1.0f);
            //GL.Translate(0.0f, -2.0f, 0f);
            //Glu.Sphere(obj, .5, 4, 2);//绘制八面体
            //GL.PopMatrix();
        }

        //绘制圆环轮胎体(圆环体半径,圆环体段的侧面数,圆环体段数)
        void DrawTorus(double Radius = 3, double TubeRadius = 1, int Sides = 6, int Rings = 10)
        {
            double sideDelta = 2.0 * Math.PI / Sides;
            double ringDelta = 2.0 * Math.PI / Rings;
            double theta = 0;
            double cosTheta = 1.0;
            double sinTheta = 0.0;

            double phi, sinPhi, cosPhi;
            double dist;            

            for (int i = 0; i < Rings; i++)
            {
                double theta1 = theta + ringDelta;
                double cosTheta1 = Math.Cos(theta1);
                double sinTheta1 = Math.Sin(theta1);
                GL.Color3(cosTheta1, 0.0f, sinTheta1);
                GL.Begin(BeginMode.QuadStrip);
                phi = 0;
                for (int j = 0; j <= Sides; j++)
                {
                    phi = phi + sideDelta;
                    cosPhi = Math.Cos(phi);
                    sinPhi = Math.Sin(phi);
                    dist = Radius + (TubeRadius * cosPhi);

                    GL.Normal3(cosTheta * cosPhi, sinTheta * cosPhi, sinPhi);
                    GL.Vertex3(cosTheta * dist, sinTheta * dist, TubeRadius * sinPhi);

                    GL.Normal3(cosTheta1 * cosPhi, sinTheta1 * cosPhi, sinPhi);
                    GL.Vertex3(cosTheta1 * dist, sinTheta1 * dist, TubeRadius * sinPhi);
                }
                GL.End();
                theta = theta1;
                cosTheta = cosTheta1;
                sinTheta = sinTheta1;
            }
        }

        //绘制1坐标轴,含有箭头:
        private void DrawOLineInGL(double azimuth, double elevation, float length, float width, Color color, bool arrow)
        {
            float colorR, colorG, colorB;
            float x, y, z;

            colorR = color.R / 255.0f;
            colorG = color.G / 255.0f;
            colorB = color.B / 255.0f;

            x = length * (float)Math.Cos(elevation) * (float)Math.Cos(azimuth);
            y = length * (float)Math.Cos(elevation) * (float)Math.Sin(azimuth);
            z = length * (float)Math.Sin(elevation);

            GL.LineWidth(width);
            GL.Begin(BeginMode.Lines);
                GL.Color3(colorR, colorG, colorB);
                GL.Vertex3(0.0f, 0.0f, 0.0f);
                GL.Vertex3(x, y, z); //三轴末端位置
            GL.End();

            if (arrow)
            {
                float vx, vy, vz;
                vx = xMul(y, z, 0, 1);
                vy = xMul(z, x, 1, 0);
                vz = xMul(x, y, 0, 0);

                GL.LineWidth(1.0f);
                GL.PushMatrix();//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< 
                
                GL.Translate(x, y, z); //坐标轴箭头所在位置
                    if (isInLimits(elevation, Math.PI / 2, Math.PI / 2 * 3))
                    {
                        GL.Rotate((float)(90 - elevation / Math.PI * 180), vx, vy, vz); //上半球?
                    }
                    else
                    {
                        GL.Rotate(-(float)(90 - elevation / Math.PI * 180), vx, vy, vz);//下半球?
                    }
                    Glu.Cylinder(Glu.NewQuadric(), 0.03, 0.0, 0.2, 20, 5);//圆锥形作箭头
                    //Glu.Sphere(Glu.NewQuadric(), 0.05, 12, 12);//球体作箭头
                GL.PopMatrix();//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            }
        }

        #endregion

        #region  基本函数

        //组件初始化:
        public Axles3D()
        {
            InitializeComponent();

            AngleY = 30;
            AngleZ = -30;
        }

        //自适应窗口大小:
        private void Axles3D_Resize(object sender, EventArgs e)
        {
            if (_loaded)
            {                
                DrawAll();
            }
        }

        //调用DrawAll():
        private void Axles3D_Paint(object sender, PaintEventArgs e)
        {
            if (_loaded)
            {                
                DrawAll();                
                
            }
        }
 
        //区域限制(中间: p, 最小: p_2, 最大: p_3):
        private bool isInLimits(double p, double p_2, double p_3)
        {
            return ((p_2 <= p) && (p <= p_3));  // p_2 <= p <= p_3
        }

        //2X2行列式计算(正对角线之积与反对角线之积的差):
        private float xMul(float x1, float x2, float y1, float y2)
        {
            return x1 * y2 - x2 * y1;
        }

        //半球范围判断:
        bool Hemishere(int x, int y, int d, float[] v)
        {
            float z;
            //计算x, y坐标:
            v[0] = (float)x * 2.0f - (float)d;
            v[1] = (float)d - (float)y * 2.0f;
            //计算z坐标:
            z = d * d - v[0] * v[0] - v[1] * v[1];
            if (z < 0)
            {
                return false;
            }
            v[2] = (float)Math.Sqrt(z);
            //单位化:
            v[0] /= (float)d;
            v[1] /= (float)d;
            v[2] /= (float)d;            
            return true;
        }

        //获取正方形长度:
        int GetSquareLength()
        {
            return this.Bounds.Width > this.Bounds.Height ? this.Bounds.Width : this.Bounds.Height;
        }

        //Axles3D类加载:
        private void Axles3D_Load(object sender, EventArgs e)
        {
            _loaded = true;
        }

        //引用Win32 API函数(第三方DLL要写入全路径,系统DLL不必):
        #region user32.dll

        [DllImport("user32.dll", EntryPoint = "ShowWindow")]
        public static extern Int32 ShowWindow(Int32 hwnd, Int32 nCmdShow);
        public const Int32 SW_SHOW = 5; public const Int32 SW_HIDE = 0;

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        private static extern Int32 SystemParametersInfo(Int32 uAction, Int32 uParam, ref Rectangle lpvParam, Int32 fuWinIni);
        public const Int32 SPIF_UPDATEINIFILE = 0x1;
        public const Int32 SPI_SETWORKAREA = 47;
        public const Int32 SPI_GETWORKAREA = 48;

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern Int32 FindWindow(string lpClassName, string lpWindowName);
        
        #endregion
        
        #endregion

        //通过三点找圆心:
        private P3D getO(P3D s, P3D m, P3D e)
        {
            double xsm = m.x - s.x;                   //(xsm,ysm,zsm)为向量SM
            double ysm = m.y - s.y;
            double zsm = m.z - s.z;

            double xme = e.x - m.x;                       //(xme,yme,zme)为向量ME
            double yme = e.y - m.y;
            double zme = e.z - m.z;

            double xp = s.x + (m.x - s.x) / 2;             //(xp,yp,zp)为SM中点
            double yp = s.y + (m.y - s.y) / 2;
            double zp = s.z + (m.z - s.z) / 2;

            double xq = m.x + (e.x - m.x) / 2;             //(xq,yq,zq)为SM中点
            double yq = m.y + (e.y - m.y) / 2;
            double zq = m.z + (e.z - m.z) / 2;

            double XN = ysm * zme - yme * zsm;          //(XN,YN1,ZN)为向量N,其值为向量SM和向量ME的差乘
            double YN1 = xme * zsm - xsm * zme;
            double ZN = xsm * yme - xme * ysm;
            double D_N = Math.Sqrt(XN * XN + YN1 * YN1 + ZN * ZN);
            double xn = XN / D_N;
            double yn1 = YN1 / D_N;
            double zn = ZN / D_N;

            double A11 = yme * zn - zme * yn1;          //矩阵A代数余子式
            double A12 = xn * zme - xme * zn;
            double A13 = xme * yn1 - xn * yme;
            double A21 = yn1 * zsm - ysm * zn;
            double A22 = xsm * zn - xn * zsm;
            double A23 = xn * ysm - xsm * yn1;
            double A31 = ysm * zme - yme * zsm;
            double A32 = xme * zsm - xsm * zme;
            double A33 = xsm * yme - xme * ysm;
            double D_A = xsm * yme * zn - xsm * yn1 * zme - xme * ysm * zn + xn * ysm * zme + xme * yn1 * zsm - xn * yme * zsm;//矩阵A行列式
            double b1 = xp * xsm + yp * ysm + zp * zsm;
            double b2 = xq * xme + yq * yme + zq * zme;
            double b3 = s.x * xn + s.y * yn1 + s.z * zn;

            P3D O = new P3D();

            O.x = (A11 * b1 + A21 * b2 + A31 * b3) / D_A;           //O为空间圆弧的圆心坐标
            O.y = (A12 * b1 + A22 * b2 + A32 * b3) / D_A;
            O.z = (A13 * b1 + A23 * b2 + A33 * b3) / D_A;

            double xoe = e.x - O.x;
            double yoe = e.y - O.y;
            double zoe = e.z - O.z;
            double R2 = xoe * xoe + yoe * yoe + zoe * zoe;
            double R1 = Math.Sqrt(R2);                             //R1为空间圆弧的半径

            return O;
        }

        //截屏保存:
        void prtScn()
        {
            //获得当前屏幕的分辨率:  
            Screen scr = Screen.PrimaryScreen;
            Rectangle rc = scr.Bounds;
            int iWidth = rc.Width;
            int iHeight = rc.Height - 80;
            //创建一个和屏幕一样大的Bitmap:              
            Image myImage = new Bitmap(iWidth, iHeight);
            //从一个继承自Image类的对象中创建Graphics对象: 
            Graphics g = Graphics.FromImage(myImage);
            //抓屏并拷贝到myimage里:  
            g.CopyFromScreen(new Point(0, 25), new Point(0, 0), new Size(iWidth, iHeight));
            //保存为文件:  
            string fn = "../../" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg";
            myImage.Save(fn,System.Drawing.Imaging.ImageFormat.Jpeg);
            MessageBox.Show(fn + "---文件已经生成");//防止连续生成
            //this.Title = fn + "---文件已经生成";
        }


        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@


        #region 测试瞎玩

        public void DrawCicurlar()
        {
            GL.Begin(BeginMode.LineStrip);
            double r = 2;
            for (double i = -2500; i < 2500; i=i+1)
            {
                r = r - 0.0004;
                GL.Vertex3(r * (float)Math.Sin(i * Math.PI / 180),  r* (float)Math.Cos(i * Math.PI / 180), i / 1000);
                //GL.Color3(Math.Sin(i),Math.Tan(i), Math.Cos(i));
            }
            GL.End();
        }


        public static double getSpeed(double days)
        {
            return planetRotateSpeed * (365 / days);
        }


        public void DrawOrbit(double r)
        {
            GL.Color3(Color.White);
            GL.Begin(BeginMode.LineStrip);
            GL.LineWidth(0.01f);

            for (double i = -180; i < 181; i = i +1)
            {
                GL.Vertex3(r * (float)Math.Sin(i * Math.PI / 180), r * (float)Math.Cos(i * Math.PI / 180), 0 );
                //GL.Color3(Math.Sin(i),Math.Tan(i), Math.Cos(i));
            }
            GL.End();
        }


        public void DrawMoonOrbit(double xc, double yc, double r, double vx, double vy, double vz, double anger)
        {
            GL.PushMatrix();
            GL.Rotate(anger, vx, vy, vz);
            GL.Color3(Color.White);

            GL.Begin(BeginMode.LineStrip);
            GL.LineWidth(0.01f);
            for (double i = -180; i < 181; i = i + 1)
            {
               
                GL.Vertex3(r * (float)Math.Sin(i * Math.PI / 180)+xc, r * (float)Math.Cos(i * Math.PI / 180)+yc, 0);
               
                //GL.Color3(Math.Sin(i),Math.Tan(i), Math.Cos(i));
            }
            GL.End();
            GL.PopMatrix();
        }

        public void DrawPipe()
        {
            GL.Begin(BeginMode.LineStrip);
            double r = 2;
            for (double i = -2500; i < 2500; i = i + 1)
            {
                GL.Vertex3(r * (float)Math.Sin(i * Math.PI / 180), r * (float)Math.Cos(i * Math.PI / 180), i / 1000);
                //GL.Color3(Math.Sin(i),Math.Tan(i), Math.Cos(i));
            }
            GL.End();
        }



        public void GenerateStars(int amount) {
            List<Vector3> stars = new List<Vector3>();
            Random r = new Random();
            int count = 0;

            while (count != amount)
            {
                Vector3 p = default(Vector3);
                p.X = (r.Next(1,1000000)) * (float)Math.Pow(-1, r.Next());
                p.Y = (r.Next(1,1000000)) * (float)Math.Pow(-1, r.Next());
                p.Z = (r.Next(1, 1000000)) * (float)Math.Pow(-1, r.Next());
                //if (Math.Pow(Math.Pow(p.X, 2) + Math.Pow(p.Y, 2) + Math.Pow(p.Z, 2), 1 / 3f) > 10)
                //{
                    stars.Add(p);
                    count++;
                //}
            }

            //GL.Begin(BeginMode.Points);
            //GL.Color3(Color.White);
            //GL.PointSize(1);
            //{
                foreach (var item in stars)
                {
                    DrawBody(item.X,item.Y,item.Z,1,2,0,0,0,0,Color.White);
                }

            //}
            GL.End();

        }




        #endregion

    }
}
