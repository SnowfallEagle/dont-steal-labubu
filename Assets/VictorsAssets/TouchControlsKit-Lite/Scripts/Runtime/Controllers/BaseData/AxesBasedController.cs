/********************************************
 * Copyright(c): 2018 Victor Klepikov       *
 *                                          *
 * Profile: 	 http://u3d.as/5Fb		    *
 * Support:      http://smart-assets.org    *
 ********************************************/


using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TouchControlsKit
{
    [Serializable]
    public sealed class Axis
    {
        public const int DIGITS = 2;

        public bool enabled = true
                  , inverse = false;

        public float value { get; private set; }

        // SetValue
        public void SetValue( float value )
        {
            this.value = enabled ? ( float )Math.Round( ( double )value, 3 ) : 0f;
        }
    };

    public abstract class AxesBasedController : ControllerBase
    {
        [Range( 1f, 10f )]
        public float sensitivity = 1f;

        [Label( "Lag" )]
        public bool axesLag = false;

        [Range( 5f, 25f )]
        public float axesLagSpeed = 10f;

        [Label( "X - [ Horizontal ]" )]
        public Axis axisX = new Axis();
        [Label( "Y - [ Vertical ]" )]
        public Axis axisY = new Axis();

        
        [SerializeField]
        bool showTouchZone = true;

        protected Vector2 defaultPosition, currentPosition, currentDirection;
        protected Vector2 lastPosition, deltaPosition;
        protected bool isFirstTouch = true;

        // Get Delta Position
        public Vector2 GetDeltaPosition()
        {
            if (!touchDown)
                return Vector2.zero;

            return deltaPosition;
        }

        // Update
        protected override void Update()
        {
            base.Update();
            
            if (touchDown)
            {
                if (isFirstTouch)
                {
                    lastPosition = currentPosition;
                    isFirstTouch = false;
                    deltaPosition = Vector2.zero;
                }
                else
                {
                    deltaPosition = currentPosition - lastPosition;
                    lastPosition = currentPosition;
                }
            }
            else
            {
                deltaPosition = Vector2.zero;
                isFirstTouch = true;
            }
        }

        // Update Position
        protected override void UpdatePosition(Vector2 touchPos)
        {
            base.UpdatePosition(touchPos);
            currentPosition = touchPos;
        }
        
        // Show TouchZone
        public bool ShowTouchZone
        {
            get { return showTouchZone; }
            set
            {
                if( showTouchZone == value )
                    return;

                showTouchZone = value;
                OnApplyShowTouchZone();
            }
        }
        // ShowHide TouchZone
        [ApplyMethod]
        protected virtual void OnApplyShowTouchZone()
        {
            baseImage.color = ( showTouchZone && visible ) ? GetActiveColor( baseImageColor ) : Color.clear;
        }

        // OnApply ActiveColors
        protected override void OnApplyActiveColors()
        {
            if( showTouchZone ) {
                base.OnApplyActiveColors();
            }            
        }

        // OnApply Visible
        protected override void OnApplyVisible()
        {
            OnApplyShowTouchZone();
        }

                
        // ResetAxes
        protected void ResetAxes()
        {
            SetAxes( 0f, 0f );
        }

        // Set Axis
        protected void SetAxes( Vector2 axes )
        {
            SetAxes( axes.x, axes.y );
        }
        // Set Axis
        protected void SetAxes( float x, float y )
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            x = axisX.inverse ? -x : x;
            y = axisY.inverse ? -y : y;

            if( axesLag )
            {
                if( axisX.enabled )
                {
                    StopCoroutine( "SmoothAxisX" );
                    StartCoroutine( "SmoothAxisX", x );                    
                }
                else
                    axisX.SetValue( 0f );

                if( axisY.enabled )
                {
                    StopCoroutine( "SmoothAxisY" );
                    StartCoroutine( "SmoothAxisY", y );
                }
                else
                    axisY.SetValue( 0f );
            }
            else
            {
                axisX.SetValue( x );
                axisY.SetValue( y );
            }
        }

        // Smooth AxisX
        private IEnumerator SmoothAxisX( float targetValue )
        {
            while( Math.Round( ( double )axisX.value, Axis.DIGITS ) != Math.Round( ( double )targetValue, Axis.DIGITS ) )
            {
                axisX.SetValue( Mathf.Lerp( axisX.value, targetValue, Time.smoothDeltaTime * axesLagSpeed ) );
                yield return null;
            }

            axisX.SetValue( targetValue );
        }
        // Smooth AxisY
        private IEnumerator SmoothAxisY( float targetValue )
        {
            while( Math.Round( ( double )axisY.value, Axis.DIGITS ) != Math.Round( ( double )targetValue, Axis.DIGITS ) )
            {
                axisY.SetValue( Mathf.Lerp( axisY.value, targetValue, Time.smoothDeltaTime * axesLagSpeed ) );
                yield return null;
            }

            axisY.SetValue( targetValue );
        }

        // Control Reset
        protected override void ControlReset()
        {
            base.ControlReset();
            ResetAxes();
            touchDown = false;
            touchId = -1;
        }        
    };
}