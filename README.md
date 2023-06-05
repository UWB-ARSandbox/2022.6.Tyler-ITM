# SAR GPS Track Processing Tool

**Welcome to the SAR GPS Track Processing Tool repository.** 

This tool is designed to expedite the GPS track processing in Search and Rescue (SAR) missions. 
By swiftly calculating the Total Track Line Length (TLL), it enhances the efficiency of SAR operations. 

This repository houses the source code for four key components integral to this tool.

# Contents:

Source/WaSARxCLI
- This package within the Source directory provides a command-line interface for our tool. It enhances the tool's accessibility and usability for those who prefer or require a command-line interface.

Source/WaSARxGUI
- The WaSARxGUI package provides a graphical user interface for our tool. This makes the tool accessible to a wider range of users who prefer visual interfaces.

Source/IntersectionSupport

- This package houses the algorithm that determines the intersection between the GPS track and the defined search area. It forms the backbone of our tool, enabling the precise calculation of the Total Track Line Length (TLL) within search areas.

Source/HeightEstimate

- HeightEstimate is an experimental package being developed alongside the main project. It queries elevation values for coordinate points, utilizing the IntersectionSupport algorithm layer. It aims to enhance terrain data processing and coverage estimation capabilities.

Source/Executables

- This directory houses the executable files for the CLI and GUI versions of our tool. These are ready-to-run programs that allow users to quickly start using the SAR GPS Track Processing Tool.

About   
- This directory contains the Capstone Paper and Presentation, providing a detailed overview of the repository and its structure.

Your contribution and feedback are welcome. Feel free to explore, use, and contribute to the development of this tool.