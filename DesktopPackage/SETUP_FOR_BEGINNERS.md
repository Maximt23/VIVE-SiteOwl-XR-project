# Unity Setup for Complete Beginners
# Follow this EXACTLY - every click explained

## BEFORE YOU START (Prerequisites)

You need these installed:
1. Unity Hub (you're downloading this now)
2. Git (we'll check this)
3. Our project files

## STEP 0: Check Git (2 minutes)

Open Command Prompt (Windows key, type "cmd", press Enter)

Type this:
```
git --version
```

If you see "git version 2.x.x" - GREAT, skip to Step 1
If you see "not recognized" - You need to install Git first:
   - Go to: https://git-scm.com/download/win
   - Download and install (all default settings)
   - Restart Command Prompt
   - Try "git --version" again

## STEP 1: Get Our Project (5 minutes)

In Command Prompt, type these commands ONE AT A TIME:

```cmd
cd %USERPROFILE%\Documents
```

```cmd
git clone https://github.com/Maximt23/VIVE-SiteOwl-XR-project.git
```

Wait for it to finish (you'll see "done" at the end)

```cmd
cd VIVE-SiteOwl-XR-project
```

```cmd
dir
```

You should see folders like: Assets, UnityProject, README.md

## STEP 2: Open Unity Hub (10 minutes)

1. Click Windows Start button
2. Type "Unity Hub" 
3. Click "Unity Hub" app when it appears
4. Wait for it to fully load (may take a minute)

## STEP 3: Add Project to Unity Hub (2 minutes)

In Unity Hub:
1. Look at left sidebar
2. Click "Projects" (if not already selected)
3. Click blue button "Open"
4. Navigate to: Documents > VIVE-SiteOwl-XR-project > UnityProject
5. Click "Select Folder"
6. Unity Hub will show "VIVE-SiteOwl-XR-Capture" in your projects list

## STEP 4: Install Unity Version (20-30 minutes, ONE TIME ONLY)

1. In Unity Hub, click "Installs" on left sidebar
2. Look for "2022.3.x LTS" in the list
   - If you see it: GREAT, skip to Step 5
   - If NOT: Continue below

To install Unity:
1. Click blue button "Install Editor"
2. Select "2022.3 LTS" from the list
3. Click "Next"
4. CHECK THESE BOXES:
   ☐ Android Build Support
   ☐ Android SDK & NDK Tools
   ☐ OpenJDK
5. Click "Install"
6. WAIT (20-30 minutes, go get coffee)

## STEP 5: Open Project (5 minutes)

1. In Unity Hub, click "Projects" on left sidebar
2. Find "VIVE-SiteOwl-XR-Capture" in the list
3. Look at the Unity version under it
4. Click on the project name
5. Unity Editor will open (first time takes 5-10 minutes to import)
6. You'll see a gray screen with a loading bar at bottom
7. Wait for "Importing assets" to finish

## STEP 6: Build the Scene (3 minutes)

Once Unity is open and ready:

1. Look at the TOP MENU BAR
2. Click "Tools" (between "Window" and "Help")
3. A dropdown menu appears
4. Hover over "CCTV Survey" (don't click yet)
5. A submenu appears
6. Click "🔨 Build Working Scene"
7. A popup asks "Build Scene?"
8. Click "Build Scene"
9. Wait 10-20 seconds
10. Another popup says "Scene Built Successfully!"
11. Click "Open Scene Now"

## STEP 7: Verify Scene (2 minutes)

You should now see:
- Gray background (scene view)
- Some colored cubes in the middle (test cameras)
- A white grid on the ground
- On the right side: Inspector panel showing "XR Cctv Rig"

If you see this: SUCCESS!

## STEP 8: Test in Editor (2 minutes)

1. Look at top center of Unity
2. Find the PLAY button (▶️ triangle icon)
3. Click the PLAY button
4. Game view opens (shows what headset sees)
5. Loading screen appears briefly
6. Camera list appears with 5 cameras
7. Click one camera name
8. Click CAPTURE button
9. Watch progress text at bottom
10. Click PLAY button again to stop

## STEP 9: Build APK (10 minutes)

1. Click "File" in top menu
2. Click "Build Settings..."
3. Window pops up
4. In platform list on left, click "Android"
5. Click "Switch Platform" button (bottom right)
   - Wait 2-5 minutes for switch
6. Click "Add Open Scenes" button
7. You should see "CctvSurvey_Working" in the list
8. Click "Build And Run"
9. Choose where to save the APK (pick Desktop for easy finding)
10. Wait 5-10 minutes for build
11. If headset is connected via USB, it will install automatically!

## STEP 10: Deploy to Headset (if Build And Run didn't work)

1. Put on VIVE XR Elite
2. Go to Settings > System > Developer
3. Turn ON "USB Debugging"
4. Plug USB-C cable into headset and PC
5. On headset, click "Allow" when prompted
6. On PC, open Command Prompt
7. Type:
   ```
   cd %USERPROFILE%\Desktop
   adb install CCTV_Survey_XR.apk
   ```
8. App appears in headset library!

## TROUBLESHOOTING

### "I don't see Tools menu"
- You need to wait for Unity to finish loading
- Look at bottom of Unity for "Importing..." progress
- Wait until that's done, then Tools will appear

### "Build Scene button is gray"
- Unity hasn't compiled the scripts yet
- Wait 30 seconds
- Try again

### "It says Android SDK not found"
- Go back to Step 4
- Make sure you installed "Android Build Support"
- May need to restart Unity

### "Git clone failed"
- Check your internet connection
- Make sure Git is installed (Step 0)
- Try the HTTPS URL: https://github.com/Maximt23/VIVE-SiteOwl-XR-project.git

## NEED MORE HELP?

If you're stuck at ANY step:
1. Take a screenshot (Windows key + Shift + S)
2. Tell me exactly which step number you're on
3. I'll walk you through it

## WHAT YOU'LL HAVE AT THE END

✅ Unity project open
✅ Working scene built
✅ APK file for headset
✅ App installed on VIVE XR Elite
✅ Ready to survey cameras!

**GOOD LUCK! You've got this! 💪**
