# DoseRateEditor

![image](https://user-images.githubusercontent.com/78000769/226069013-a34d6001-5132-40af-a9d9-9218b1879bd5.png)

![image](https://user-images.githubusercontent.com/78000769/226070099-f5304c74-735c-42e7-998a-194466d78563.png)

![image](https://user-images.githubusercontent.com/78000769/226110675-884f5268-f19c-4adf-ab0a-3b94b20abd2b.png)
### Features
* Copy current arc plan into a new course with new dynamic dose rate and gantry speed independent of aperture 
* Used to convert non-coplanar conformal arcs with static dose rate and gantry speed into dynamic dose rate and gantry speed plans
* If applied to plans already with dynaamic dose rate, edited dose rate likely to be undesirable
* Precomplied executables for Eclipse 15.6 - 18 availible in [Releases](https://github.com/Varian-Innovation-Center/MAAS-DoseDynamicArcs/releases)
* [/ExampleNoncoplanarBeamTemplates](https://github.com/Varian-Innovation-Center/MAAS-DoseDynamicArcs/tree/main/ExamplePlanTemplates)
* Installation steps similar to [PlanScoreCard QuickStart}(https://github.com/Varian-Innovation-Center/MAAS-PlanScoreCard/blob/main/BasicInstallQuickStart.md) and [Install Part2](https://github.com/Varian-Innovation-Center/MAAS-PlanScoreCard/blob/main/InstallGuidePart2IntoSystemScriptsDirectory.md)

### Instructions For Use
* Create non-coplanar arc beam plan (see templates above)
* Add MLC to beams either static or dynamic (control points every 2 degree recommended for dynamic)
* Create static MLC aperature for each beam or use fit to structure for dynamic MLC beams
<br/>&nbsp;&nbsp;* Aperature required for Millennium 120 MLC
<br/>&nbsp;&nbsp;![image](https://user-images.githubusercontent.com/78000769/229162975-5d3dff0f-b05a-4995-b9ed-eaad92c34111.png)
<br/>&nbsp;&nbsp;* Closed HD-MLC will be offered a 2.1mm opening in center two leaves ([Virtual Cone](https://www.sciencedirect.com/science/article/pii/S2452109418300368) type treatments)
<br/>&nbsp;&nbsp;![image](https://user-images.githubusercontent.com/78000769/229163577-610772eb-069b-4b64-be28-6646c7f08244.png)
* Launch DoseRateEditor (follow installation instructions above to optionally configure launcher)
* Select method to edit dose rate (currently all methods are simple sine or sine-like with extreamly similar results)
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

