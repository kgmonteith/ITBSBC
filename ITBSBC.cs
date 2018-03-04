//If you want a quick overview of how the configuration system works, take a look at SolExodus.cs
//This example was meant to recreate the functionality I displayed for the system in the original release
//however that also means that it is actually pretty complicated.

using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
namespace SBC{
    
    public class DynamicClass
    {        
        String debugString = "";
        static SteelBattalionController controller;
        vJoy joystick;
		int vJoyButtons = 39;
		bool acquired;
        const int refreshRate = 5; // Number of milliseconds between call to mainLoop
        static Random random = new Random();
        
        //int baseLineIntensity = 5;
        int maxLightIntensity = 15;

        int screen_height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
        int screen_width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
        SimpleRunningAverage x_smooth = new SimpleRunningAverage(30);
        SimpleRunningAverage y_smooth = new SimpleRunningAverage(30);

        bool trigger_down = false;
        bool lock_on_down = false;
        bool eject_down = false;

        bool main_weapon_down = false;
        int main_weapon_down_ticks = 0;
        bool weapon_con_main_down = false;
        bool weapon_con_sub_down = false;

        bool last_toggle_vt_location = false;
        bool last_toggle_buffer_material = false;
        bool last_toggle_fuel_flow_rate = false;

        bool sight_change_x = false;
        bool sight_change_y = false;
        int sight_change_trigger = 300;

        int last_controller_dial;
        int saved_gear_lever;

        int[] baseLineIntensity;
        int[] middle_switch_lights = {14,15,16,20,21,22,23,24,25,29,30,31,32,33};
        int[] first_switch_lights = {17,18,19,26,27,28};

        // Container for active flashing lights
        static List<FlashingLight> flashingLights = new List<FlashingLight>();

        //this gets called once by main program
        public void Initialize()
        {
            controller = new SteelBattalionController();
            controller.Init(refreshRate);

            baseLineIntensity = new int[35];
           
            // Simple button bindings
            controller.AddButtonKeyLightMapping(ButtonEnum.WeaponConMain, true, maxLightIntensity, SBC.Key.D1, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.WeaponConSub, true, maxLightIntensity, SBC.Key.D2, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.CockpitHatch, true, maxLightIntensity, SBC.Key.Tab, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.Ignition, true, maxLightIntensity, SBC.Key.LeftShift, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.Start, true, maxLightIntensity, SBC.Key.Space, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.FunctionF1, true, maxLightIntensity, SBC.Key.A, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.FunctionF2, true, maxLightIntensity, SBC.Key.S, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.FunctionF3, true, maxLightIntensity, SBC.Key.D, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.FunctionTankDetach, true, maxLightIntensity, SBC.Key.F, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.FunctionOverride, true, maxLightIntensity, SBC.Key.G, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.FunctionNightScope, true, maxLightIntensity, SBC.Key.H, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.FunctionManipulator, true, maxLightIntensity, SBC.Key.Z, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.FunctionLineColorChange, true, maxLightIntensity, SBC.Key.X, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.Washing, true, maxLightIntensity, SBC.Key.Q, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.Extinguisher, true, maxLightIntensity, SBC.Key.R, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.Comm1, true, maxLightIntensity, SBC.Key.Escape, false);
            controller.AddButtonKeyLightMapping(ButtonEnum.Comm5, true, maxLightIntensity, SBC.Key.F11, false);
            // Map every left side button on the monitor block to left alt
            controller.AddButtonKeyLightMapping(ButtonEnum.MultiMonOpenClose, true, maxLightIntensity, SBC.Key.LeftAlt, true);
            controller.AddButtonKeyLightMapping(ButtonEnum.MultiMonModeSelect, true, maxLightIntensity, SBC.Key.LeftAlt, true);
            controller.AddButtonKeyLightMapping(ButtonEnum.MainMonZoomIn, true, maxLightIntensity, SBC.Key.LeftAlt, true);
            // Map every right side button on the monitor block to left ctrl
            controller.AddButtonKeyLightMapping(ButtonEnum.MultiMonMapZoomInOut, true, maxLightIntensity, SBC.Key.LeftControl, true);
            controller.AddButtonKeyLightMapping(ButtonEnum.MultiMonSubMonitor, true, maxLightIntensity, SBC.Key.LeftControl, true);
            controller.AddButtonKeyLightMapping(ButtonEnum.MainMonZoomOut, true, maxLightIntensity, SBC.Key.LeftControl, true);

            controller.AddButtonKeyMapping(ButtonEnum.ToggleFilterControl, SBC.Key.LeftAlt, true);
            controller.AddButtonKeyMapping(ButtonEnum.ToggleOxygenSupply, SBC.Key.LeftControl, true);

            controller.AddButtonKeyMapping(ButtonEnum.LeftJoySightChange, SBC.Key.LeftShift, false);

            last_controller_dial = controller.TunerDial;
            saved_gear_lever = controller.GearLever;

            check_light_toggles();

            joystick = new vJoy();
            acquired = joystick.acquireVJD(1);
            joystick.resetAll();

        }

        public void check_light_toggles() {
            if(controller.GetButtonState((int)ButtonEnum.ToggleVTLocation) != last_toggle_vt_location) {
                last_toggle_vt_location = !last_toggle_vt_location;
                for(int i = 0; i <= 13; i++) {
                    baseLineIntensity[i] = last_toggle_vt_location ? 5 : 0;
                    controller.SetLEDState((ControllerLEDEnum)i, baseLineIntensity[i], false);
                }
            }
            if(controller.GetButtonState((int)ButtonEnum.ToggleBufferMaterial) != last_toggle_buffer_material) {
                last_toggle_buffer_material = !last_toggle_buffer_material;
                foreach(int i in middle_switch_lights) {
                    baseLineIntensity[i] = last_toggle_buffer_material ? 5 : 0;
                    controller.SetLEDState((ControllerLEDEnum)i, baseLineIntensity[i], false);
                }
            }
            if(controller.GetButtonState((int)ButtonEnum.ToggleFuelFlowRate) != last_toggle_fuel_flow_rate) {
                last_toggle_fuel_flow_rate = !last_toggle_fuel_flow_rate;
                foreach(int i in first_switch_lights) {
                    baseLineIntensity[i] = last_toggle_fuel_flow_rate ? 5 : 0;
                    controller.SetLEDState((ControllerLEDEnum)i, baseLineIntensity[i], false);
                }
            }
            for(int i = 0; i < baseLineIntensity.Count(); i++) {
                if(controller.GetLEDState((ControllerLEDEnum)i) < baseLineIntensity[i]) {
                    controller.SetLEDState((ControllerLEDEnum)i, baseLineIntensity[i], false);
                }
            }
        }

        // Main program calls this to know how often to call mainLoop
        public int getRefreshRate()
        {
            return refreshRate;
        }

        private void assert_weapon_state(int weapon_number) {
            if(weapon_number == 1) {
                // Weapon 1... Flash 'main'
                flashingLights.Add(new FlashingLight(ButtonEnum.WeaponConMain, baseLineIntensity[(int)ButtonEnum.WeaponConMain], 50, maxLightIntensity, 100, 3));
            } else if(weapon_number == 2) {
                // Weapon 2... Flash 'Sub'
                flashingLights.Add(new FlashingLight(ButtonEnum.WeaponConSub, baseLineIntensity[(int)ButtonEnum.WeaponConSub], 50, maxLightIntensity, 100, 3));
            }
        }

        //this gets called once every refreshRate milliseconds by main program
        public void mainLoop()
        {

            // Set mouse position
            int mouse_x = x_smooth.Add(controller.AimingX) ;
            int mouse_y = y_smooth.Add(controller.AimingY) ;
            mouse_x = (int) (((double)mouse_x / 1024.0) * screen_width);
            mouse_y = (int) (((double)mouse_y / 1024.0) * screen_height);
            MouseOperations.SetCursorPosition(mouse_x, mouse_y);

            // Set mouse clicks
            if(controller.GetButtonState((int)ButtonEnum.RightJoyFire) && !trigger_down) {
                // Trigger depressed, send mouse down
                trigger_down = true;
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftDown);
            } else if (!controller.GetButtonState((int)ButtonEnum.RightJoyFire) && trigger_down) {
                // Trigger released, send mouse up
                trigger_down = false;
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftUp);
            }
            if(controller.GetButtonState((int)ButtonEnum.RightJoyLockOn) && !lock_on_down) {
                lock_on_down = true;
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.RightDown);
            } else if (!controller.GetButtonState((int)ButtonEnum.RightJoyLockOn) && lock_on_down) {
                lock_on_down = false;
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.RightUp);
            }

            // Check sight change stick states
            if(controller.SightChangeX < sight_change_trigger && controller.SightChangeX > -1 * sight_change_trigger) {
                sight_change_x = false;
            }
            if(controller.SightChangeY < sight_change_trigger && controller.SightChangeY > -1 * sight_change_trigger) {
                sight_change_y = false;
            }
            if(controller.SightChangeX >= sight_change_trigger && !sight_change_x) {
                // Stick to the right == cycle pawns
                sight_change_x = true;
                controller.sendKeyPress(SBC.Key.Tab);
            }
            if(controller.SightChangeX <= -1 * sight_change_trigger && !sight_change_x) {
                // Stick to the left == pawn 2
                sight_change_x = true;
                controller.sendKeyPress(SBC.Key.S);
            }
            if(controller.SightChangeY >= sight_change_trigger && !sight_change_y) {
                // Stick to the bottom == pawn 3
                sight_change_y = true;
                controller.sendKeyPress(SBC.Key.D);
            }
            if(controller.SightChangeY <= -1 * sight_change_trigger && !sight_change_y) {
                // Stick to the top == pawn 1
                sight_change_y = true;
                controller.sendKeyPress(SBC.Key.A);
            }

            // Set states for weapon buttons
            if(controller.GetButtonState((int)ButtonEnum.RightJoyMainWeapon)) {
                // Main weapon depressed
                main_weapon_down = true;
                main_weapon_down_ticks++;
            } else if (!controller.GetButtonState((int)ButtonEnum.RightJoyMainWeapon) && main_weapon_down) {
                // Main weapon released
                if(main_weapon_down_ticks * refreshRate < 100) {
                    // Short press == weapon one
                    assert_weapon_state(1);
                    controller.sendKeyPress(SBC.Key.D1);
                } else {
                    // Long press == weapon two
                    assert_weapon_state(2);
                    controller.sendKeyPress(SBC.Key.D2);
                }
                main_weapon_down = false;
                main_weapon_down_ticks = 0;
            }
            if(controller.GetButtonState((int)ButtonEnum.WeaponConMain)) {
                weapon_con_main_down = true;
            } else if(!controller.GetButtonState((int)ButtonEnum.WeaponConMain) && weapon_con_main_down) {
                weapon_con_main_down = false;
                assert_weapon_state(1);
            }
            if(controller.GetButtonState((int)ButtonEnum.WeaponConSub)) {
                weapon_con_sub_down = true;
            } else if(!controller.GetButtonState((int)ButtonEnum.WeaponConSub) && weapon_con_sub_down) {
                weapon_con_sub_down = false;
                assert_weapon_state(2);
            }

            // Gear level chooses active pawn
            int gear_lever = controller.Scaled.GearLever;
            if (gear_lever != saved_gear_lever)
            {
                switch (gear_lever) {
                    case 3:
                        controller.sendKeyPress(SBC.Key.A);
                        flashingLights.Add(new FlashingLight(ButtonEnum.FunctionF1, baseLineIntensity[(int)ButtonEnum.FunctionF1], 30, 12, 100, 2));
                        break;
                    case 2:
                        controller.sendKeyPress(SBC.Key.S);
                        flashingLights.Add(new FlashingLight(ButtonEnum.FunctionF2, baseLineIntensity[(int)ButtonEnum.FunctionF2], 30, 12, 100, 2));
                        break;
                    case 1:
                        controller.sendKeyPress(SBC.Key.D);
                        flashingLights.Add(new FlashingLight(ButtonEnum.FunctionF3, baseLineIntensity[(int)ButtonEnum.FunctionF3], 30, 12, 100, 2));
                        break;
                    case -1:
                        controller.sendKeyPress(SBC.Key.Z);
                        flashingLights.Add(new FlashingLight(ButtonEnum.FunctionManipulator, baseLineIntensity[(int)ButtonEnum.FunctionManipulator], 30, 12, 100, 2));
                        break;
                    case -2:
                        controller.sendKeyPress(SBC.Key.X);
                        flashingLights.Add(new FlashingLight(ButtonEnum.FunctionLineColorChange, baseLineIntensity[(int)ButtonEnum.FunctionLineColorChange], 30, 12, 100, 2));
                        break;
                }
                saved_gear_lever = gear_lever;
            }

            // Tuner dial cycles through pawns
            if(last_controller_dial != controller.TunerDial)
			{
				controller.sendKeyPress(SBC.Key.Tab);
				last_controller_dial = controller.TunerDial;
			}

            // Eject button, JUMP BACK IN THE TIMELINE, WE FUCKED UP BAD
            if(controller.GetButtonState((int)ButtonEnum.Eject)) {
                controller.SetLEDState(ControllerLEDEnum.EmergencyEject, maxLightIntensity);
                eject_down = true;
            } else if(!controller.GetButtonState((int)ButtonEnum.Eject) && eject_down) {
                controller.SetLEDState(ControllerLEDEnum.EmergencyEject, baseLineIntensity[(int)ControllerLEDEnum.EmergencyEject]);
                controller.sendKeyPress(SBC.Key.BackSpace);
                MouseOperations.SetCursorPosition((screen_width/2)-65, (screen_height/2)+60);
                System.Threading.Thread.Sleep(50);
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftDown);
                System.Threading.Thread.Sleep(50);
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftUp);
                eject_down = false;
                GO_CRAZY();
            }

            // Flash flashing lights
            for (int i = flashingLights.Count - 1; i >= 0; i--)
            {
                bool finished = flashingLights[i].poll(refreshRate);
                if (finished)
                {
                    flashingLights.RemoveAt(i);
                }
            }
            check_light_toggles();
            controller.RefreshLEDState();
        }

        //new necessary function used for debugging purposes
        public String getDebugString()
        {
            return debugString;
        }

        //this gets called at the end of the program and must be present, as it cleans up resources
        public void shutDown()
        {
            controller.UnInit();
            joystick.Release(1);
        }

        public void GO_CRAZY() {
            // LIGHT UP LIKE A DAMNED CHRISTMAS TREE
            List<int> lights = new List<int>(Enumerable.Range(0,35));
            List<int> relight = new List<int>();
            while(lights.Count > 0) {
                for(int i=0; i<=maxLightIntensity; i++) {
                    foreach(int light in lights) {
                        controller.SetLEDState((ControllerLEDEnum)light, i, false);
                    }
                    foreach(int light in relight) {
                        if(i <= baseLineIntensity[light]) {
                            controller.SetLEDState((ControllerLEDEnum)light, i, false);
                        }
                    }
                    controller.RefreshLEDState();
                }
                relight.Clear();
                for(int i=maxLightIntensity; i>0; i--) {
                    foreach(int light in lights) {
                        controller.SetLEDState((ControllerLEDEnum)light, i, false);
                    }
                    controller.RefreshLEDState();
                }
                // Pop some elements
                for(int i = 0; i < Math.Min(lights.Count, random.Next(5,8)); i++) {
                    int element = random.Next(lights.Count);
                    relight.Add(lights[element]);
                    lights.Remove(lights[element]);
                }
                
            }
        }


        // Ported from KSPSBC

        // Unthreaded flashing light class, requires polling to update state
        public class FlashingLight
        {
            public ControllerLEDEnum light;
            public ButtonEnum button;
            public int iterations;
            public int lowLevel;
            public int highLevel;
            public List<Tuple<int, int>> lightStates;  // index 0 is lowest state -- tuple order: <intensity, duration>

            int duration;
            int lightLevel = 0; // index of lightState; 0 == lowest
            public FlashingLight(ButtonEnum b, List<Tuple<int, int>> ls, int i)
            {
                this.light = controller.GetLightForButton(b);
                this.button = b;
                this.lightStates = ls;
                this.iterations = i;

                this.duration = this.lightStates[0].Item2;
                controller.SetLEDState(this.light, this.lightStates[0].Item1);
            }

            public FlashingLight(ButtonEnum b, int ll, int ld, int hl, int hd, int i)
            {
                this.light = controller.GetLightForButton(b);
                this.button = b;
                this.lightStates = new List<Tuple<int, int>>() {
                    new Tuple<int, int>(ll, ld),
                    new Tuple<int, int>(hl, hd)
                };
                this.iterations = i;

                this.duration = this.lightStates[0].Item2;
                controller.SetLEDState(this.light, this.lightStates[0].Item1);
            }

            public bool poll(int elapsed)
            {
                this.duration -= elapsed;
                if (this.duration <= 0)
                {
                    if (this.iterations > 0 && this.lightLevel == this.lightStates.Count - 1)
                    {
                        --this.iterations;
                    }
                    else if (this.iterations == 0)
                    {
                        controller.SetLEDState(this.light, this.lightStates[0].Item1, false);
                        return true;
                    }
                    // Change LED state
                    if (!controller.GetButtonState((int)button))
                    {
                        this.lightLevel++;
                        if (this.lightLevel >= this.lightStates.Count)
                        {
                            this.lightLevel = 0;
                        }
                        controller.SetLEDState(this.light, this.lightStates[lightLevel].Item1, false);
                        this.duration = this.lightStates[lightLevel].Item2;
                    }
                }
                return false;
            }

            public void reset()
            {
                this.duration = this.lightStates[0].Item2;
                controller.SetLEDState(this.light, this.lightStates[0].Item1);
            }
        }
    }






    

    // Taken from blogs and stackoverflow

    public class SimpleRunningAverage 
    { 
        int _size; 
        int[] _values = null; 
        int _valuesIndex = 0; 
        int _valueCount = 0; 
        int _sum = 0; 

        public SimpleRunningAverage(int size) 
        { 
            System.Diagnostics.Debug.Assert(size > 0); 
            _size = Math.Max(size, 1); 
            _values = new int[_size]; 
        } 

        public int Add(int newValue) 
        { 
            // calculate new value to add to sum by subtracting the 
            // value that is replaced from the new value; 
            int temp = newValue - _values[_valuesIndex]; 
            _values[_valuesIndex] = newValue; 
            _sum += temp; 

            _valuesIndex++; 
            _valuesIndex %= _size; 
            
            if (_valueCount < _size) 
            _valueCount++; 

            return _sum / _valueCount; 
        } 
    }
    
    public class MouseOperations
    {
        [Flags]
        public enum MouseEventFlags
        {
            LeftDown = 0x00000002,
            LeftUp = 0x00000004,
            MiddleDown = 0x00000020,
            MiddleUp = 0x00000040,
            Move = 0x00000001,
            Absolute = 0x00008000,
            RightDown = 0x00000008,
            RightUp = 0x00000010
        }

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);      

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out MousePoint lpMousePoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        public static void SetCursorPosition(int x, int y) 
        {
            SetCursorPos(x, y);
        }

        public static void SetCursorPosition(MousePoint point)
        {
            SetCursorPos(point.X, point.Y);
        }

        public static MousePoint GetCursorPosition()
        {
            MousePoint currentMousePoint;
            var gotPoint = GetCursorPos(out currentMousePoint);
            if (!gotPoint) { currentMousePoint = new MousePoint(0, 0); }
            return currentMousePoint;
        }

        public static void MouseEvent(MouseEventFlags value)
        {
            MousePoint position = GetCursorPosition();

            mouse_event
                ((int)value,
                position.X,
                position.Y,
                0,
                0)
                ;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MousePoint
        {
            public int X;
            public int Y;

            public MousePoint(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
    }
}

