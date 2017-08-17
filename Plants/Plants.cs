﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;

namespace groundhog
{

    class PlantFactory
    {

        public static Dictionary<string, string> parseToDictionary(string headers, string values)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            string[] splitKeys = headers.Split(',');
            string[] splitValues = values.Split(',');

            int i;
            for (i = 0; i < splitKeys.Length; i = i + 1)
            {
                if (splitValues[i].Trim() != "")
                {
                    dictionary.Add(splitKeys[i].Trim(), splitValues[i].Trim());
                }
            }

            return dictionary;
        }

        public static Tuple<PlantSpecies, string> parseFromDictionary(Dictionary<string, string> speciesInstance)
        {
            string warnings = "";
            // Naming
            string speciesName, commonName, indigenousName;
            // Lifespan
            double timetoMaturity, deathRate;
            // Placement
            double requiredSpacingRadius;
            // Form
            double initialCrownRadius, matureCrownRadius, varianceCrownRadius;
            double initialRootRadius, matureRootRadius, varianceRootRadius;
            double initialHeight, matureHeight, varianceHeight;
            double initialTrunkRadius, matureTrunkRadius, varianceTrunkRadius;
            // Aesthetics
            string displayRGB;

            // Naming
            if (speciesInstance.ContainsKey("Species Name")) {
                speciesName = speciesInstance["Species Name"];
            } else {
                speciesName = "Unnamed";
                warnings += "no Species name; ";
            }
            if (speciesInstance.ContainsKey("Common Name")) {
                commonName = speciesInstance["Common Name"];
            } else {
                commonName = "Unnamed";
                warnings += "no Common name; ";
            }
            if (speciesInstance.ContainsKey("Indigenous Name")) {
                indigenousName = speciesInstance["Indigenous Name"];
            } else {
                indigenousName = "Unnamed";
                warnings += "no Indigenous name; ";
            }

            // Lifespan
            if (speciesInstance.ContainsKey("Time to Maturity")) {
                timetoMaturity = Convert.ToDouble(speciesInstance["Time to Maturity"]);
            } else {
                timetoMaturity = 100.0;
                warnings += "no Time to Maturity; ";
            }

            // Placement
            if (speciesInstance.ContainsKey("Spacing Radius")) {
                requiredSpacingRadius = Convert.ToDouble(speciesInstance["Spacing Radius"]);
            } else {
                requiredSpacingRadius = 1000.0;
                warnings += "no Spacing Radius; ";
            }

            // Form
            if (speciesInstance.ContainsKey("Initial Crown Radius")) {
                initialCrownRadius = Convert.ToDouble(speciesInstance["Initial Crown Radius"]);
            } else {
                initialCrownRadius = 10.0;
                warnings += "no Initial Crown Radius; ";
            }            
            if (speciesInstance.ContainsKey("Mature Crown Radius")) {
                matureCrownRadius = Convert.ToDouble(speciesInstance["Mature Crown Radius"]);
            } else {
                matureCrownRadius = 10.0;
                warnings += "no Mature Crown Radiu; s";
            } 
            if (speciesInstance.ContainsKey("Crown Variance")) {
                varianceCrownRadius = Convert.ToDouble(speciesInstance["Crown Variance"]);
            } else {
                varianceCrownRadius = 10.0;
                warnings += "no Crown Variance; ";
            }          
            if (speciesInstance.ContainsKey("Initial Trunk Radius"))
            {
                initialTrunkRadius = Convert.ToDouble(speciesInstance["Initial Trunk Radius"]);
            }
            else
            {
                initialTrunkRadius = 10.0;
                warnings += "no Initial Trunk Radius; ";
            }
            if (speciesInstance.ContainsKey("Mature Height"))
            {
                matureTrunkRadius = Convert.ToDouble(speciesInstance["Mature Trunk Radius"]);
            }
            else
            {
                matureTrunkRadius = 100.0;
                warnings += "no Mature Trunk Radius; ";
            }
            if (speciesInstance.ContainsKey("Trunk Variance"))
            {
                varianceTrunkRadius = Convert.ToDouble(speciesInstance["Trunk Variance"]);
            }
            else
            {
                varianceTrunkRadius = 10.0;
                warnings += "no Trunk Variance; ";
            }
            if (speciesInstance.ContainsKey("Initial Height"))
            {
                initialHeight = Convert.ToDouble(speciesInstance["Initial Height"]);
            }
            else
            {
                initialHeight = 1000.0;
                warnings += "no Initial Height; ";
            }
            if (speciesInstance.ContainsKey("Mature Height"))
            {
                matureHeight = Convert.ToDouble(speciesInstance["Mature Height"]);
            }
            else
            {
                matureHeight = 1000.0;
                warnings += "no Mature Height; ";
            }
            if (speciesInstance.ContainsKey("Height Variance"))
            {
                varianceHeight = Convert.ToDouble(speciesInstance["Height Variance"]);
            }
            else
            {
                varianceHeight = 10.0;
                warnings += "no Height Variance; ";
            }

            if (speciesInstance.ContainsKey("Initial Root Radius")) 
            {
                initialRootRadius = Convert.ToDouble(speciesInstance["Initial Root Radius"]);
            } 
            else 
            {
                initialRootRadius = 1000.0;
                warnings += "no Initial Root Radius; ";
            }
            if (speciesInstance.ContainsKey("Mature Root Radius")) {
                matureRootRadius = Convert.ToDouble(speciesInstance["Mature Root Radius"]);
            } 
            else 
            {
                matureRootRadius = 1000.0;
                warnings += "no Mature Root Radius; ";
            }
            if (speciesInstance.ContainsKey("Root Variance")) {
                varianceRootRadius = Convert.ToDouble(speciesInstance["Root Variance"]);
            } 
            else 
            {
                varianceRootRadius = 10.0;
                warnings += "no Root Variance; ";
            }
            
            // Aesthetics
            if (speciesInstance.ContainsKey("Display RGB")) {
                displayRGB = speciesInstance["Display RGB"];
            } 
            else 
            {
                displayRGB = "100,100,100";
                warnings += "no Display RGB; ";
            }

            PlantSpecies initialisedSpecies = new PlantSpecies(
                speciesName: speciesName, commonName: commonName, indigenousName: indigenousName,                        
                timetoMaturity: timetoMaturity,                     
                requiredSpacingRadius: requiredSpacingRadius,         
                initialCrownRadius: initialCrownRadius, matureCrownRadius: matureCrownRadius, varianceCrownRadius: varianceCrownRadius,
                initialRootRadius: initialRootRadius, matureRootRadius: matureRootRadius, varianceRootRadius: varianceRootRadius,                    
                initialHeight: initialHeight, matureHeight: matureHeight, varianceHeight: varianceHeight,                        
                initialTrunkRadius: initialTrunkRadius, matureTrunkRadius: matureTrunkRadius, varianceTrunkRadius: varianceTrunkRadius,
                displayRGB: displayRGB                
            );

            return Tuple.Create(initialisedSpecies, warnings);
        }


    }

        
    class PlantSpecies : GH_Param<IGH_Goo>
    {
        // Naming
        public readonly string speciesName, commonName, indigenousName;
        // Lifespan
        private readonly double timetoMaturity;
        // Placement
        private readonly double requiredSpacingRadius;
        // Form
        private readonly double initialCrownRadius, matureCrownRadius, varianceCrownRadius;
        private readonly double initialRootRadius, matureRootRadius, varianceRootRadius;
        private readonly double initialHeight, matureHeight, varianceHeight;
        private readonly double initialTrunkRadius, matureTrunkRadius, varianceTrunkRadius;
        // Aesthetics
        private readonly string displayRGB;

        // Get current state
        private double getGrowth(double initial, double eventual, double time)
        {
            double annualRate = (eventual - initial) / this.timetoMaturity;
            double grownTime = Math.Min(time, this.timetoMaturity);
            double grownState = (grownTime * annualRate) + initial;
            return grownState;
        }

        // Get geometry
        public Circle getCrown(Point3d location, double time)
        {
            double height = getGrowth(this.initialHeight, this.matureHeight, time);
            double radius = getGrowth(this.initialCrownRadius, this.matureCrownRadius, time);
            Point3d canopyLocation = new Point3d(location.X, location.Y, (location.Z + height));
            return new Circle(canopyLocation, radius);
        }
        public Circle getRoot(Point3d location, double time)
        {
            double radius = getGrowth(this.initialRootRadius, this.matureRootRadius, time);
            return new Circle(location, radius);
        }
        public Circle getTrunk(Point3d location, double time)
        {
            double radius = getGrowth(this.initialTrunkRadius, this.matureTrunkRadius, time);
            return new Circle(location, radius);
        }
        public Circle getSpacing(Point3d location)
        {
            return new Circle(location, this.requiredSpacingRadius);
        }

        public System.Drawing.Color getColour()
        {
            // Parse the comma seperated string into an array of ints
            string[] colorComponents = this.displayRGB.Split(',');
            int[] colorValues = new int[3];
            for (int i = 0; i < colorComponents.Length; i++)
            {
                colorValues[i] = Convert.ToInt16(colorComponents[i].Trim());
            }

            System.Drawing.Color colour = System.Drawing.Color.FromArgb(colorValues[0], colorValues[1], colorValues[2]);
            return colour;
        }
        public GH_String getLabel()
        {
            return new GH_String(this.speciesName);
        }


        #region properties
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("2d268bdc-ecaa-4cf7-815a-c8111d1798d7"); }
        }
        public override string ToString()
        {
            return "groundhog Plant Species (" + speciesName + ")";
        }
        #endregion

        #region casting
        protected override IGH_Goo InstantiateT()
        {
            return new GH_ObjectWrapper();
        }
        #endregion

        // Init
        public PlantSpecies(
            string speciesName, string commonName, string indigenousName,
            double timetoMaturity, 
            double requiredSpacingRadius,
            double initialCrownRadius, double matureCrownRadius, double varianceCrownRadius,
            double initialRootRadius, double matureRootRadius, double varianceRootRadius,
            double initialHeight, double matureHeight, double varianceHeight,
            double initialTrunkRadius, double matureTrunkRadius, double varianceTrunkRadius,
            string displayRGB
        )
            : base(new GH_InstanceDescription("Plant param", "P", "TODO:", "Params"))
        {
            // Naming
            this.speciesName = speciesName;
            this.commonName = commonName;
            this.indigenousName = indigenousName;
            // Lifespan
            this.timetoMaturity = timetoMaturity;
            // Placement
            this.requiredSpacingRadius = requiredSpacingRadius;
            // Form
            this.initialCrownRadius = initialCrownRadius;
            this.matureCrownRadius = matureCrownRadius;
            this.varianceCrownRadius = varianceCrownRadius;
            this.initialRootRadius = initialRootRadius;
            this.matureRootRadius = matureRootRadius;
            this.varianceRootRadius = varianceRootRadius;
            this.initialHeight = initialHeight;
            this.matureHeight = matureHeight;
            this.varianceHeight = varianceHeight;
            this.initialTrunkRadius = initialTrunkRadius;
            this.matureTrunkRadius = matureTrunkRadius;
            this.varianceTrunkRadius = varianceTrunkRadius;
            // Aesthetics
            this.displayRGB = displayRGB;
        }

    }

}
