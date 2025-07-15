## MAAS-DoseDynamicArcs
### a collection of tools for making spherical dose dirstubutions by varying the dose rate and gantry speed of non-coplanar arc treatment plans
<br>
# DoseRateEditor
### A general-purpose tool which employs mathmatical functions to edit the dose rate and gantry speed of existing static speed and rate conformal arc treatment plans

![image](https://user-images.githubusercontent.com/78000769/226069013-a34d6001-5132-40af-a9d9-9218b1879bd5.png)

![image](https://user-images.githubusercontent.com/78000769/226070099-f5304c74-735c-42e7-998a-194466d78563.png)

![image](https://user-images.githubusercontent.com/78000769/226110675-884f5268-f19c-4adf-ab0a-3b94b20abd2b.png)
### Features
* Copy current arc plan into a new course with new dynamic dose rate and gantry speed independent of aperture 
* Used to convert non-coplanar conformal arcs with static dose rate and gantry speed into dynamic dose rate and gantry speed plans
* If applied to plans already with dynaamic dose rate, edited dose rate likely to be undesirable
* Precomplied executables for Eclipse 15.6 - 18 availible in [Releases](https://github.com/Varian-Innovation-Center/MAAS-DoseDynamicArcs/releases)
* [/ExampleNoncoplanarBeamTemplates](https://github.com/Varian-Innovation-Center/MAAS-DoseDynamicArcs/tree/main/ExamplePlanTemplates)
* Installation steps similar to [PlanScoreCard QuickStart](https://github.com/Varian-Innovation-Center/MAAS-PlanScoreCard/blob/main/BasicInstallQuickStart.md) and [Install Part2](https://github.com/Varian-Innovation-Center/MAAS-PlanScoreCard/blob/main/InstallGuidePart2IntoSystemScriptsDirectory.md)

### Instructions For Use
* Create non-coplanar arc beam plan (see templates above)
* Add MLC to beams either static or dynamic
<br/>![image](https://user-images.githubusercontent.com/78000769/229165841-bdc84bdb-b1b2-4ad3-98b6-c37f8e5dc202.png)
<br/>&nbsp;&nbsp;* Control points every 2 degrees recommended for dynamic
* Create MLC aperature for each beam manually or use fit to structure for dynamic MLC beams
<br/>![image](https://user-images.githubusercontent.com/78000769/229167371-bdd8c716-e799-4b9c-b3a1-5786d6e81507.png)
<br/>&nbsp;&nbsp;* Aperature required for Millennium 120 MLC
<br/>&nbsp;&nbsp;![image](https://user-images.githubusercontent.com/78000769/229162975-5d3dff0f-b05a-4995-b9ed-eaad92c34111.png)
<br/>&nbsp;&nbsp;* Closed HD-MLC will be offered a 2.1mm opening in center two leaves ([Virtual Cone](https://www.sciencedirect.com/science/article/pii/S2452109418300368) type treatments)
<br/>&nbsp;&nbsp;![image](https://user-images.githubusercontent.com/78000769/229163577-610772eb-069b-4b64-be28-6646c7f08244.png)
* Launch DoseRateEditor (follow installation instructions above to optionally configure launcher)
* Select method to edit dose rate (currently all methods are simple sine or sine-like with extremely similar results)
<br/>&nbsp;&nbsp;* Optionally, click the preview boxes to see what the resulting Dose Rate and Gantry Speed
<br/>&nbsp;&nbsp;* Delta MU (the combined effect of dose rate and gantry speed) preview is not yet implemented
* Click Edit DR for all fields to create a new dynamic dose rate and gantry speed version of selected plan 
<br/>&nbsp;&nbsp;* Static MLC plans will be switched to dynamic and static aperatures will be copied to all control points
<br/>&nbsp;&nbsp;* Dynamic MLC plans only have their dose rate and gantry speed edited
* Open newly created course and plan as directed and calculate dose
<br/>![image](https://user-images.githubusercontent.com/78000769/229161916-ecf2a082-6a74-4194-b1ac-628014377f8a.png)
<br/>&nbsp;&nbsp;* Optionally, open the newly created plan in DoseRateEditor to verify expected result (and view Delta MU plot)

### Future Roadmap / Wishlist
- [ ] Working preview of Delta MU
- [ ] Add/enable addtional intelligenent dose rate modification methods (not only simple sine-like functions)
- [ ] Show persistent warning when selected plan has no dose calulated (not current pop-up message box)
- [ ] CT slice view move to isocenter, show structures, show dose, drag dose?
- [ ] When block support is added to ESAPI, support cones?
<br>
<br>
# VirtualCones
### A special-purpose tool which leverages precomputed dose rate and gantry speed tables (Maps) to implemnet [Popple Et al's Virtual Cone method](https://www.advancesradonc.org/article/S2452-1094(18)30036-8/pdf)

<img width="988" height="794" alt="image" src="https://github.com/user-attachments/assets/2e44bdd9-560e-4683-8d66-111886065c9c" />

### Instructions For Use
# ***WARNING: A FULL END-TO-END VALIDATION must be completed for each configuration of beam template, dose rate map, and other settings. *** 


* Unzip the file in [Releases](https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS-DoseDynamicArcs/releases) to a location that has access to Eclipse. A normal location would be a subfolder of Published scripts
  - \\SERVER\va_data$\ProgramData\Vision\PublishedScripts\DoseRateEditor-V1.0.0.X-07-14-2025.12-31-2025-EclipseVXXX\virtualcones
* Launch "Scripts" from the Planning menu in Eclipse, click "Change Folder..." to the above location, then click OK and finally set VirtualConeLauncher.cs as a favorite
<img width="570" height="590" alt="image" src="https://github.com/user-attachments/assets/e488e438-496d-4727-be46-2a43106f1343" />
* Use a text editor to open the Settings.XML file found in the installation directory.

<img width="981" height="418" alt="image" src="https://github.com/user-attachments/assets/7fa77b48-d82a-4113-b5c4-700d20bb1969" />
* Each value is in millimeters.
* GapPair > GapSize & NumberOfLeaves: The script will create an MLC-defined aperture. The aperture is the central “NumberOfLeaves” MLC pairs by the “GapSize.”
  - NumberOfLeaves must be even.
* “X” and “Y” represent the jaw size. In the above image, X & Y are set to 20 mm.
* “EnableSlidingLeaf” – The field must have a technique of VMAT to modulate the dose. Setting this to true causes leaf-pair at the very edge to move during treatment.
* SlidingLeafGapSize is the gap between the ends of the moving leaf pair.
* Set these values and save the file.
* Approve the script.
- In Eclipse, go to Tools > Scripts Approvals.
- Click Register New Script, and choose the “VC_SecondCheck.exe” from the install folder.
- Highlight the newly added script and click “Approve”
- Enter your credentials and click Authorize.
- Finally, click Apply and then OK.
- WARNING: Do NOT click the “X” button to close the screen; it will not save the approval.

# Dose Rate Maps
* Dose rates are assigned using Gantry vs. Normalized Dose Rate maps found in the “Maps” sub directory.
* Each row is a comma-separated Gantry Angle and a normalized dose rate pairing.
* Although not required, it is suggested to enter a value for each 2 degrees, starting with 0 through 360.
* Save the file as a *.txt file in the Maps subdirectory. The name of the file will be used in the tool, so use a descriptive file name.
* The script will interpolate between map values by Gantry Angle.
* Here is an example of the first entries of a sinusoidal dose rate map.

# Dose Rate Map Tips
* Not all dose-rate maps are deliverable. You may need to modify the dose map based on energy, dose rate, arc length, prescription dose, number of beams, etc.
The most likely failure is a too low MU/Deg (i.e. <0.1 MU/Deg for TrueBeam).

# Template Creation
* The script can create beams based on a template. One template is provided; however, the user may wish to create different templates
* In Eclipse, create a plan
* Add each beam and set its geometry
- You do not need to set the desired MLC, Energy, Fluence Mode, Dose Rate, or isocenter here
- The beams should be arcs
* Set the Field Id’s as you would like them to appear in the final plan
* With the template plan in the context, open the script
* The plan should automatically be selected
- If the plan is not selected upon opening the script, you can do so within the script
* Enter the name of the new template in the Beam Template Creation section, at the bottom
- When ready, click the “Create Beam Template with the Id:” button
* The new beam template will be available in the Insert Beams drop down:
* By default, Virtual Cone Size is filtered by Plan Energy
- if the selected plan’s first beam has an energy of 6X-FFF, then only Virtual Cone Sizes with 6X-FFF and Beam Templates with
6X-FFF will be available.
- To remove the filter when building templates, check the box for “Beam Template Creation
Mode
* Select the virtual cone size.
* Select the dose rate map and set the field weight for each beam.
* After each action, the beam template collection is saved along with any changes the user has made, even if that template is not currently selected.
- Create Beam Template with Id
- Duplicate Beam Template with Id
* Save Template
* After making changes, ensure that you have click “Save Template.”

## Overview
The user starts by creating a “staging plan.” This plan contains information the script will use to create
the Virtual Cone plan. The “Insert Beams” function of the script will insert the selected beams and set
their geometries and modulated dose rates.
```
## Staging Plan
* Create a staging course and plan.
- The prescription from this plan will be copied to the Virtual Cone Plan.
* Add a beam.
- This beam will be used by the script to determine the MLC, Energy, Fluence Mode, Dose Rate, Isocenter, and Machine.
- Set each of the following as it should be in the final plan: MLC, Energy and Fluence Mode, Isocenter, Machine
- Nothing more needs to be done (e.g. do not set blocking).

## Run Script
* With the staging plan in context, open the script.
* The information will be automatically loaded in the script.
- *Note: You can modify machine/course/plan selection in the script if necessary.

### Insert Beams
* Choose the beam template you wish to use.
- The script comes with the standard beam configuration.
- If your clinic uses Varian-IEC, then use VarianIEC.
- If your clinic uses IEC61217, then use IEC
* Review the Settings.
- Settings must be configured in the Settings.xml file.
* Review the Virtual Cone Size selection, field weightings, and dose rate map selections.
* Click the “Insert Beams” button.
- Using the information from the staging plan and the chosen template, a new plan is created.
- If a course with Id of “VirtualCone” is not available, it will create one.
- The plan will be placed in “VirtualCone” course, with an Id of “VirtualCone”.
- If the Id is already taken, an integer will be added to the end andincremented as necessary (e.g. VirtualCone, VirtualCone1)
* A progress window will pop-up: When complete.....
- The progress window will indicate 100% and say “Done!”
- A Message Box, stating the Course Id and Plan Id, will appear. You may click OK to clear the message.
- The Course Id and Plan Id in the selection space will be automatically updated.
- You may click OK and close the script.
* Reload the patient in Eclipse, and select the new plan.
* Calculate the dose and follow the review/approval procedures.
* To quickly check that the script has properly placed the beams and applied dose rates, review the model view. For a sinusoidal dose rate map, a crescent shaped dose-rate track indicates it has been properly applied.
