﻿using System;
using System.Diagnostics;
using Rendering;
using MathSupport;
using System.Collections.Generic;
using Utilities;
using OpenTK;

namespace _063animation
{
  public class FormSupport
  {
    /// <summary>
    /// Initialize animation parameters.
    /// </summary>
    public static void InitializeParams ( out string name )
    {
      name = "Josef Pelikán";

      Form1 f = Form1.singleton;

      f.textParam.Text = "slant=true,prefix=out,bg=[0.05;0.0;0.0],fg=[1.0;1.0;0.8]";

      // single frame:
      f.ImageWidth = 640;
      f.ImageHeight = 480;
      f.numericSupersampling.Value = 4;

      // animation:
      f.numFrom.Value = (decimal)0.0;
      f.numTo.Value = (decimal)10.0;
      f.numFps.Value = (decimal)25.0;
    }

    /// <summary>
    /// Initialize (optional) animation data.
    /// </summary>
    public static object getData ( string param )
    {
      return new AnimationData( param );
    }

    /// <summary>
    /// Initialize image function.
    /// </summary>
    public static IImageFunction getImageFunction ( string param, object data )
    {
      return new Animation( param, data );
    }

    /// <summary>
    /// Initialize image synthesizer (responsible for raster image computation).
    /// </summary>
    public static IRenderer getRenderer ( string param, IImageFunction imf )
    {
      Form1 f = Form1.singleton;

      if ( f.superSampling > 1 )
      {
        SupersamplingImageSynthesizer sis = new SupersamplingImageSynthesizer();
        sis.ImageFunction = imf;
        sis.Supersampling = f.superSampling;
        sis.Jittering = 1.0;
        return sis;
      }
      SimpleImageSynthesizer s = new SimpleImageSynthesizer();
      s.ImageFunction = imf;
      return s;
    }
  }

  /// <summary>
  /// Pilot implementation of time-dependent image function.
  /// </summary>
  public class Animation : IImageFunction, ITimeDependent
  {
    /// <summary>
    /// Starting (minimal) time in seconds.
    /// </summary>
    public double Start
    {
      get;
      set;
    }

    /// <summary>
    /// Ending (maximal) time in seconds.
    /// </summary>
    public double End
    {
      get;
      set;
    }

    protected double time;

    protected double frequency = 100.0;

    protected double width = 1.0;

    protected double height = 1.0;

    protected double center;

    protected double mul;

    protected void setup ()
    {
      center = 0.5 * width;
      mul = frequency / width;
    }

    /// <summary>
    /// Looks around to an infinite checkerboard.
    /// One complete turn for the whole time interval.
    /// </summary>
    protected virtual void setTime ( double newTime )
    {
      Debug.Assert( Start != End );

      time = newTime;

      // change the view azimuth:
      double pr = (time - Start) / (End - Start);
      frequency = 100.0 + pr * 9900.0;
      setup();
    }

    /// <summary>
    /// Current time in seconds.
    /// </summary>
    public double Time
    {
      get
      {
        return time;
      }
      set
      {
        setTime( value );
      }
    }

    /// <summary>
    /// Domain width.
    /// </summary>
    public double Width
    {
      get
      {
        return width;
      }
      set
      {
        width = value;
        setup();
      }
    }

    /// <summary>
    /// Domain height.
    /// </summary>
    public double Height
    {
      get
      {
        return height;
      }
      set
      {
        height = value;
        setup();
      }
    }

    /// <summary>
    /// Support object (optional).
    /// </summary>
    protected object data = null;

    /// <summary>
    /// Cached text param.
    /// </summary>
    protected string param = null;

    /// <summary>
    /// Slanted checkerboard?
    /// </summary>
    protected bool slant = false;

    /// <summary>
    /// Background color.
    /// </summary>
    protected double[] bg = new double[] { 0.0, 0.0, 0.0 };

    /// <summary>
    /// Foreground color.
    /// </summary>
    protected double[] fg = new double[] { 1.0, 1.0, 1.0 };

    /// <summary>
    /// Computes one image sample. Internal integration support.
    /// </summary>
    /// <param name="x">Horizontal coordinate.</param>
    /// <param name="y">Vertical coordinate.</param>
    /// <param name="color">Computed sample color.</param>
    /// <returns>Hash-value used for adaptive subsampling.</returns>
    public virtual long GetSample ( double x, double y, double[] color )
    {
      long ord = 0L;
      if ( !Geometry.IsZero( y ) )
      {
        double u = mul * (x - center) / y;
        double v = frequency / y;
        ord = slant ? (long)(Math.Round( u + v ) + Math.Round( u - v )) :
                      (long)(Math.Round( u )     + Math.Round( v ));
      }

      Array.Copy( (ord & 1L) == 0 ? fg : bg, color, fg.Length );
      return ord;
    }

    /// <summary>
    /// Clone all the time-dependent components, share the others.
    /// </summary>
    /// <returns></returns>
    public virtual object Clone ()
    {
      ITimeDependent datatd = data as ITimeDependent;
      Animation c = new Animation( param, (datatd == null) ? data : datatd.Clone() );
      c.Start  = Start;
      c.End    = End;
      c.Time   = Time;
      c.Width  = width;
      c.Height = height;
      c.bg     = bg;
      c.fg     = fg;
      c.slant  = slant;
      return c;
    }

    public Animation ( string par, object d =null )
    {
      data   = d;
      Start  = 0.0;
      End    = 10.0;
      time   = 0.0;
      Width  = 1.0;
      Height = 1.0;
      SetParams( par );
    }

    /// <summary>
    /// Update animation parameters.
    /// </summary>
    /// <param name="param">User-provided parameter string.</param>
    void SetParams ( string par )
    {
      // input params:
      Dictionary<string, string> p = Util.ParseKeyValueList( param = par );
      if ( p.Count == 0 )
        return;

      // slant version of the checkerboard:
      Util.TryParse( p, "slant", ref slant );

      // background color:
      Vector3 col = Vector3.Zero;
      if ( Geometry.TryParse( p, "bg", ref col ) )
        bg = new double[] { col.X, col.Y, col.Z };

      // foreground color:
      if ( Geometry.TryParse( p, "fg", ref col ) )
        fg = new double[] { col.X, col.Y, col.Z };
    }
  }

  /// <summary>
  /// Animation data (optional).
  /// </summary>
  public class AnimationData    // : ITimeDependent
  {
    public AnimationData ( string param )
    {
      // !!!{{ TODO: initialize the animation data

      // !!!}}
    }
  }
}
