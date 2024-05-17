#0. Import data.
import sys
import pandas as pd
import numpy as np
import math
import glob
import os

data = pd.DataFrame()

# Old import method
# for arg in sys.argv:
#     if arg == sys.argv[0]:
#         continue
#     file_path = arg
#     new_data = pd.read_csv(file_path)   
#     data = pd.concat([data, new_data], ignore_index=True)

# New import method
# Download data from onedrive and set the directory containing the CSV files
directory = '../Original Data'
# Specify the range of participants to include
participant_start = 5
participant_end = 28
for participant_id in range(participant_start - 1, participant_end + 1):
    file_pattern = os.path.join(directory, f'{participant_id}_selections.csv')
    files = glob.glob(file_pattern)
    for file_path in files:
        if os.path.basename(file_path) == f'{participant_id}_selections.csv':
            new_data = pd.read_csv(file_path)
            data = pd.concat([data, new_data], ignore_index=True)

pd.set_option('display.max_colwidth', None)

# typecast TargetSize
data['TargetSize'] = data['TargetSize'].astype(str)

#0.1 Check the data types

print(data.dtypes.to_string())

#0.2 Check whether there are any missing values

""" grouped = data.groupby(['ParticipantID','Movement', 'ReferenceFrame', 'TargetSize'])
print(grouped.size().to_string()) """

def missing_values():
    grouped = data.groupby(['ParticipantID','Movement', 'ReferenceFrame', 'TargetSize'])
    print(grouped.size().to_string())

grouped_by_movement = data.groupby(['Movement'])
if len(grouped_by_movement.size()) != 3:
    print('There are not all three types of movements')
    missing_values()
    sys.exit(1)
if grouped_by_movement.filter(lambda x: len(x) % 84 != 0).shape[0] > 0:
    print('There are missing / more values (Movement)')
    missing_values()
    sys.exit(1)
grouped_by_reference_frame = data.groupby(['ReferenceFrame'])
if len(grouped_by_reference_frame.size()) != 3:
    print('There are not all three types of reference frames')
    missing_values()
    sys.exit(1)
if grouped_by_reference_frame.filter(lambda x: len(x) % 84 != 0).shape[0] > 0:
    print('There are missing / more values (ReferenceFrame)')
    missing_values()
    sys.exit(1)
grouped_by_target_size = data.groupby(['TargetSize'])
if len(grouped_by_target_size.size()) != 4:
    print('There are not all four types of target sizes')
    missing_values()
    sys.exit(1)
if grouped_by_target_size.filter(lambda x: len(x) % 63 != 0).shape[0] > 0:
    print('There are missing / more values (TargetSize)')
    missing_values()
    sys.exit(1)

# 0.5 Compute distances from target to selection point
def calculate_distance(x1, y1, x2, y2):
    distance = math.sqrt(math.pow(x1 - x2, 2) + math.pow(y1 - y2, 2))
    return distance

# Apply the function to each row
data['DistanceCM'] = data.apply(
    lambda row: 100*calculate_distance(row['AbsoluteSelectionPositionX'], row['AbsoluteSelectionPositionY'], row['AbsoluteTargetPositionX'], row['AbsoluteTargetPositionY']),
    # lambda row: calculate_distance(row['LocalSelectionPositionX'], row['LocalSelectionPositionY'], row['AbsoluteTargetPositionX'], row['AbsoluteTargetPositionY']),
    axis=1
)

#1. Apply the equations for the per-row calculation of dx & ae from the 'First_2_Pilots.xlsx' file to the data you have

def dx(current_row, previous_row):
    if previous_row is None:  # This checks for the first row, where there is no previous row
        return 0
    else:
        # Compute a: Distance from previous target to current target =KVROD(POTENS(I2-I3;2)+POTENS(J2-J3;2))
        targetXSquaredDifference = math.pow(previous_row['AbsoluteTargetPositionX'] - current_row['AbsoluteTargetPositionX'],2)
        targetYSquaredDifference = math.pow(previous_row['AbsoluteTargetPositionY'] - current_row['AbsoluteTargetPositionY'],2)
        a = math.sqrt(targetXSquaredDifference + targetYSquaredDifference)

        # Compute b: Distance from current target center to current selection point (Accuracy) =KVROD(POTENS(K3-I3;2)+POTENS(L3-J3;2))   
        selectionXSquaredDifference = math.pow(current_row['AbsoluteSelectionPositionX'] - current_row['AbsoluteTargetPositionX'],2)
        selectionYSquaredDifference = math.pow(current_row['AbsoluteSelectionPositionY'] - current_row['AbsoluteTargetPositionY'],2)
        b = math.sqrt(selectionXSquaredDifference + selectionYSquaredDifference)

        # Compute c: Distance from previous target center to current selection point =KVROD(POTENS(I2-K3;2)+POTENS(J2-L3;2))
        selectionXSquaredDifference = math.pow(previous_row['AbsoluteTargetPositionX'] - current_row['AbsoluteSelectionPositionX'],2)
        selectionYSquaredDifference = math.pow(previous_row['AbsoluteTargetPositionY'] - current_row['AbsoluteSelectionPositionY'],2)
        c = math.sqrt(selectionXSquaredDifference + selectionYSquaredDifference)

        # compute dx =(S3 * S3 - R3 * R3 - Q3 * Q3) / (2 * Q3)
        temp = math.pow(c,2) - math.pow(b,2) - math.pow(a,2)
        dx = temp / (2*a)
        return dx

def ae(current_row, previous_row):
    if previous_row is None:  # This checks for the first row, where there is no previous row
        return 0
    else:
        # Compute a: Distance from previous target to current target =KVROD(POTENS(I2-I3;2)+POTENS(J2-J3;2))
        targetXSquaredDifference = math.pow(previous_row['AbsoluteTargetPositionX'] - current_row['AbsoluteTargetPositionX'],2)
        targetYSquaredDifference = math.pow(previous_row['AbsoluteTargetPositionY'] - current_row['AbsoluteTargetPositionY'],2)
        a = math.sqrt(targetXSquaredDifference + targetYSquaredDifference)
        return a + current_row['dx']


# # Applying the function to each row
data['dx'] = data.apply(lambda row: dx(row, data.loc[row.name - 1] if row.name > 0 else None), axis=1)
data['ae'] = data.apply(lambda row: ae(row, data.loc[row.name - 1] if row.name > 0 else None), axis=1)
# print(data.to_string())

#2. Correct Walking and TargetSize so SPSS would work later: Walking 0 -> Standing, 1 -> Walking; TargetSizeCM = TargetSize * 100

# This is already done in the data

#3. Convert the file to *.csv format to store values only

# This is already done in the data

#4. Filter using the 'Data' tab in Excel and delete the rows where the 'SelectionDuration' value equals 0 (that is the first selection and as a result an idle selection)

data = data[data['SelectionDuration'] != 0]

#5. Delete all unnecessary columns, i.e. all except for ParticipantID, conditions, ActiveTargetIndex, Success, SelectionDuration -> MT, dx, and ae

data = data[['ParticipantID', 'Movement', 'CircleDirection', 'ReferenceFrame', 'TargetSize', 'DistanceCM', 'ActiveTargetIndex', 'Success', 'SelectionDuration', 'dx', 'ae']]
data = data.rename(columns={'SelectionDuration': 'MT'})

#6. Convert data using SPSS' Data -> Restructure from the long to wide format (Identifier = ParticpantID + all conditions, Index Vars = ActiveTargetIndex). Don't forget to sort data here or at the step 9

#data = data.pivot_table(index=['ParticipantID', 'Movement', 'ReferenceFrame', 'TargetSize'], columns='ActiveTargetIndex').reset_index()

#7. Calculate average SuccessRate, MT / 1000 to convert from ms to s, and Ae, and standard deviation of dx (SDx) and delete all the 'smth.1-7' columns

data = data.groupby(['ParticipantID', 'Movement', 'CircleDirection', 'ReferenceFrame', 'TargetSize'], dropna=False).agg({'Success': 'mean', 'MT': 'mean', 'dx': 'std', 'ae': 'mean', 'DistanceCM': 'mean'}).reset_index()
data['MT'] = data['MT'] / 1000

#8. Calculate IDe, TP and WeCM = We * 100 (because it's in meters) as described here (https://www.yorku.ca/mack/hhci2018.html, Figure 17.7)

data['WeCM']  = data['dx']*4.133*100
data['IDe'] = np.log2(data['ae']*100/data['WeCM'] + 1)
data['TP'] = data['IDe']/data['MT']

# import matplotlib.pyplot as plt
# Scatter plot of IDe vs MT
# plt.scatter(data['IDe'], data['MT'])
# plt.xlabel('IDe')  # X-axis label
# plt.ylabel('MT')  # Y-axis label
# plt.title('Scatter Plot of IDe vs MT')  # Title of the plot
# plt.grid(True)  # Adds a grid for better readability
# plt.show()  # Display the plot


#9. Deal with reference frame naming (Hand-Referenced, position only -> HandRefPos; Hand-Referenced -> HandRef; Path-Referenced, Simulated Torso -> PathRefNeck; Path-Referenced -> PathRef)

# This is already done in the data
data_art = data.copy()
grouped_for_stats = data_art.groupby(['ParticipantID','ReferenceFrame']).agg({'Success': 'mean', 'MT': 'mean', 'dx': 'std', 'ae': 'mean', 'WeCM': 'mean', 'IDe': 'mean', 'TP': 'mean'})
print(grouped_for_stats.to_string())

#10. Convert data to wide format again but now with the following parameters: Identifier = ParticpantID, Index Vars = all conditions

data = data.drop(["CircleDirection"], axis=1)
data = data.pivot_table(index=['ParticipantID'], columns=['Movement', 'ReferenceFrame', 'TargetSize']).reset_index()

# write to file
def export_csv(data, name):
    data.to_csv(str(participant_start) + "-" + str(participant_end) + "_" + name, index=False) 


data.columns = ['_'.join(col).strip() for col in data.columns.values]
data.rename(columns={'ParticipantID___': 'ParticipantID'}, inplace=True)
export_csv(data, "preprocessed.csv")
export_csv(data_art, "preprocessed_art.csv")


# Count the occurrences of "Clockwise" and "CounterClockwise" in the "CircleDirection" column
clockwise_count = (data_art['CircleDirection'] == 'Clockwise').sum()
counterclockwise_count = (data_art['CircleDirection'] == 'CounterClockwise').sum()

print(f"Number of 'Clockwise' rows: {clockwise_count}")
print(f"Number of 'CounterClockwise' rows: {counterclockwise_count}")

#----------
#For aligned ranks transformation (ART):
#1. Take the data in the long format from the step 9 above

# data_art

#2. Create a bunch of files for each dependent variable (the first column is ID followed by the columns that represent conditions, the last column in each file should contain values of one dependent variable)

tp_dependent = data_art[['ParticipantID', 'Movement', 'ReferenceFrame', 'TargetSize', 'TP']].copy()
tp_dependent["Movement"] = tp_dependent["Movement"].astype('category')
tp_dependent["ReferenceFrame"] = tp_dependent["ReferenceFrame"].astype('category')
tp_dependent["TargetSize"] = tp_dependent["TargetSize"].astype('category')

import rpy2.robjects.packages as rpackages
import rpy2.robjects as ro
from rpy2.robjects import pandas2ri
ARTool = rpackages.importr('ARTool')

with (ro.default_converter + pandas2ri.converter).context():
    r_from_pd_df = ro.conversion.get_conversion().py2rpy(tp_dependent)

#3. Use ARTool to transform data taking one file by one (https://depts.washington.edu/acelab/proj/art/). Don't forget to tick 'Want contrasts' option to conduct post-hoc tests later on. This means that you'll need to 'Align and Rank' that many times as many contrast you want, e.g. 3 for 2 independent variables, 7 for 3, etc.

ro.r('''
    f <- function(data) {
        m <- art(TP ~ Movement*ReferenceFrame*TargetSize, data=data)
        art.con(m, "Movement:ReferenceFrame")
        m$aligned.ranks
        # summary(m)
        # anova(m)
    }
''')
f = ro.globalenv['f']
print(f(r_from_pd_df))

#4. Rename columns as such: ART(Effort) for Movement -> Effort_M, ART(Effort) for Movement*ReferenceFrame-> Effort_MxR, ART-C(Effort) for Movement -> Effort-C_M, etc.
#5. Combine all output files (only relevant columns) incl. contrasts into one file in long format
#6. Convert data to wide format again but now with the following parameters: Identifier = ParticpantID, Index Vars = all conditions
#Data Analysis
#1. Check the normality of all dependent variables using the Shapiro-Wilk test. Remember that for within-subject designs the normality should be checked independently for each condition
#2. Check out the skewness. Depending on it, decide which transformation to apply, if any (https://rpubs.com/frasermyers/627589)