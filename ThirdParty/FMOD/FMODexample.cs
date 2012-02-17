/*===============================================================================================
 PlayStream Example
 Copyright (c), Firelight Technologies Pty, Ltd 2004-2011.

 This example shows how to simply play a stream, such as an mp3 or wav.
 The stream behaviour is achieved by specifying FMOD_CREATESTREAM in the call to 
 System::createSound.
 This makes FMOD decode the file in realtime as it plays, instead of loading it all at once.
 This uses far less memory, in exchange for a small runtime cpu hit.
===============================================================================================*/

using System;
using System.Diagnostics;

namespace FMOD
{
	public class Framework
	{
        public static FMOD.System  system  = null;

        public static void init()
        {
            if (system == null)
            {
                Console.WriteLine("initializing c# fmod");

                uint version = 0;
                FMOD.RESULT result;

                /*
                    Global Settings
                */
                result = FMOD.Factory.System_Create(ref system);
                ERRCHECK(result);

                result = system.getVersion(ref version);
                ERRCHECK(result);
                if (version < FMOD.VERSION.number)
                {
                    Debug.Assert(false, "Error!  You are using an old version of FMOD " + version.ToString("X") + ".  This program requires " + FMOD.VERSION.number.ToString("X") + ".");
                }

                const int MAX_SOUND_CHANNELS = 32;
                result = system.init(MAX_SOUND_CHANNELS, FMOD.INITFLAGS.SOFTWARE_OCCLUSION, (IntPtr)null);
                ERRCHECK(result);
                
                /*                               FMOD.                 Instance  Env   Diffus  Room   RoomHF  RmLF DecTm   DecHF  DecLF   Refl  RefDel   Revb  RevDel  ModTm  ModDp   HFRef    LFRef   Diffus  Densty  FLAGS */
                FMOD.REVERB_PROPERTIES massive = new FMOD.REVERB_PROPERTIES(0, 23, 0.50f, 0, 0, 0, 5.00f, 0.5f, 1.0f, 0, 0.005f, 0, 0.005f, 0.25f, 0.000f, 5000.0f, 250.0f, 50.0f, 66.0f, 0x3f);
                system.setReverbProperties(ref massive);
            }
        }

        public static void update()
        {
            FMOD.RESULT result;
            result = system.update();
            ERRCHECK(result);
        }

        public static void terminate()
        {
            FMOD.RESULT result;

            /*
                Shut down
            */
            if (system != null)
            {
                result = system.close();
                ERRCHECK(result);
                result = system.release();
                ERRCHECK(result);
                system = null;
            }
        }

        public static void resetSystem()
        {
            terminate();
            init();
        }

        private static void ERRCHECK(FMOD.RESULT result)
        {
            if (result != FMOD.RESULT.OK)
            {
                Debug.Assert(false, "FMOD error! " + result + " - " + FMOD.Error.String(result));
            }
        }
	}
}
