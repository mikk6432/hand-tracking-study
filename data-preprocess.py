#0. Import data.
import sys
import pandas as pd
import numpy as np
import scipy.stats as st
import math
import glob
import os
import matplotlib.pyplot as plt
import seaborn as sns
from statannot import add_stat_annotation

# write to file
def export_csv(data, name):
    data.to_csv(str(participant_start) + "-" + str(participant_end) + "_" + name, index=False) 



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
directory = sys.argv[1]
# Specify the range of participants to include
participant_start = 5
participant_end = 28
for participant_id in range(participant_start, participant_end + 1):
    file_pattern = os.path.join(directory, f'{participant_id}_selections.csv')
    files = glob.glob(file_pattern)
    for file_path in files:
        if os.path.basename(file_path) == f'{participant_id}_selections.csv':
            print(f'Processing participant {participant_id}')
            new_data = pd.read_csv(file_path)
            data = pd.concat([data, new_data], ignore_index=True)

pd.set_option('display.max_colwidth', None)

# typecast TargetSize
data['TargetSize'] = data['TargetSize'].astype(str)

#0.1 Check the data types

#print(data.dtypes.to_string())

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

# Apply the function to each row
data['b'] = np.sqrt((data['AbsoluteTargetPositionX'] - data['AbsoluteSelectionPositionX'])**2 + (data['AbsoluteTargetPositionY'] - data['AbsoluteSelectionPositionY'])**2)

#1. Apply the equations for the per-row calculation of dx & ae from the 'First_2_Pilots.xlsx' file to the data you have

def change(row_name_prev, row_name_curr):
    return data[row_name_curr] - data[row_name_prev].shift(1, fill_value=data[row_name_curr][0])

data['a'] = np.sqrt((change("AbsoluteTargetPositionX","AbsoluteTargetPositionX"))**2 + (change("AbsoluteTargetPositionY","AbsoluteTargetPositionY"))**2)
data['c'] = np.sqrt((change("AbsoluteTargetPositionX","AbsoluteSelectionPositionX"))**2 + (change("AbsoluteTargetPositionY","AbsoluteSelectionPositionY"))**2)
data['dx'] = (data['c'] * data['c'] - data['b'] * data['b'] - data['a'] * data['a']) / (2.0 * data['a'])
data['ae'] = data['a'] + data['dx']
#print(data.head())
#print(data.shape)
data["b-bigger-dx"] = data["dx"] > np.abs(data["dx"])
#print(data["b-bigger-dx"].value_counts())
# print(data.to_string())

#2. Correct Walking and TargetSize so SPSS would work later: Walking 0 -> Standing, 1 -> Walking; TargetSizeCM = TargetSize * 100

# This is already done in the data

#3. Convert the file to *.csv format to store values only

# This is already done in the data

#4. Filter using the 'Data' tab in Excel and delete the rows where the 'SelectionDuration' value equals 0 (that is the first selection and as a result an idle selection)

data = data[data['SelectionDuration'] != 0]
#data = data[data['ReferenceFrame'] != 'PalmWORotation']

plt.scatter(data['b'], data['SelectionDuration'], c=data['b'] < 0.08 )
ax = plt.gca()
ax.set_xlabel('Distance (m)')
ax.set_ylabel('Movement Time (ms)')
fig = plt.gcf()
plt.subplots_adjust(left=0.25, right=0.9, top=0.95, bottom=0.15)
fig.set_size_inches(3, 3.5)
fig.savefig('outliers.png', dpi=100)
plt.close()

#print((data['b'] > 0.1).value_counts())
#print((data['b'] > 0.09).value_counts())
print((data['b'] > 0.08).value_counts())
#print((data['b'] > (data['b'].mean() + 2* data['b'].std())).value_counts())
data = data[data['b'] < 0.08]
#print(len(data))


#5. Delete all unnecessary columns, i.e. all except for ParticipantID, conditions, ActiveTargetIndex, Success, SelectionDuration -> MT, dx, and ae

data = data[['ParticipantID', 'Movement', 'CircleDirection', 'ReferenceFrame', 'TargetSize', 'ActiveTargetIndex', 'Success', 'SelectionDuration', 'b', 'dx', 'ae']]
data = data.rename(columns={'SelectionDuration': 'MT'})

#6. Convert data using SPSS' Data -> Restructure from the long to wide format (Identifier = ParticpantID + all conditions, Index Vars = ActiveTargetIndex). Don't forget to sort data here or at the step 9

#data = data.pivot_table(index=['ParticipantID', 'Movement', 'ReferenceFrame', 'TargetSize'], columns='ActiveTargetIndex').reset_index()

#7. Calculate average SuccessRate, MT / 1000 to convert from ms to s, and Ae, and standard deviation of dx (SDx) and delete all the 'smth.1-7' columns
export_csv(data, "preprocessed_each.csv")
data = data.groupby(['ParticipantID', 'Movement', 'CircleDirection', 'ReferenceFrame', 'TargetSize'], dropna=False).agg({'Success': 'mean', 'MT': 'mean', 'dx': 'std', 'ae': 'mean', 'b': 'mean'}).reset_index()
data['MT'] = data['MT'] / 1000
data['SDx'] = data['dx']

#8. Calculate IDe, TP and WeCM = We * 100 (because it's in meters) as described here (https://www.yorku.ca/mack/hhci2018.html, Figure 17.7)

data['WeCM']  = data['dx']*4.133*100
data['IDe'] = np.log2(data['ae']*100/data['WeCM'] + 1)
data['TP'] = data['IDe']/data['MT']
data["DistanceCM"] = data["b"] * 100
data = data.drop(['b',"dx"], axis=1)

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
grouped_for_stats = data_art.groupby(['ReferenceFrame', "Movement"]).agg({'Success': 'mean', 'MT': 'mean', 'DistanceCM': 'mean', 'SDx': 'std', 'ae': 'mean', 'WeCM': 'mean', 'IDe': 'mean', 'TP': 'mean'})
#print(grouped_for_stats.to_string())
grouped_for_stats = data_art
grouped_for_stats['Success_STD'] = grouped_for_stats['Success']
grouped_for_stats['MT_STD'] = grouped_for_stats['MT']
grouped_for_stats['WeCM_STD'] = grouped_for_stats['WeCM']
grouped_for_stats['IDe_STD'] = grouped_for_stats['IDe']
grouped_for_stats['TP_STD'] = grouped_for_stats['TP']
original_size = len(grouped_for_stats)

#10. Convert data to wide format again but now with the following parameters: Identifier = ParticpantID, Index Vars = all conditions

data = data.drop(["CircleDirection"], axis=1)
data = data.pivot_table(index=['ParticipantID'], columns=['Movement', 'ReferenceFrame', 'TargetSize']).reset_index()

data.columns = ['_'.join(col).strip() for col in data.columns.values]
data.rename(columns={'ParticipantID___': 'ParticipantID'}, inplace=True)

#----------
# Append the extra dependent variables
new_data = pd.read_csv('5-28_additional_dependent_variables.csv')
new_data['TargetSize'] = new_data['TargetSize'].astype(str)
# Merge additional dependent variables into data_art
data_art = pd.merge(data_art, new_data, on=['ParticipantID', 'Movement', 'ReferenceFrame', 'TargetSize'], how='left')
export_csv(data, "preprocessed.csv")
export_csv(data_art, "preprocessed_art.csv")

from scipy.stats import shapiro
shapiro_test = data_art.groupby(['Movement', 'ReferenceFrame', 'TargetSize']).agg({'Success': lambda x: shapiro(x)[1], 'MT': lambda x: shapiro(x)[1], 'DistanceCM': lambda x: shapiro(x)[1], 'SDx': lambda x: shapiro(x)[1], 'ae': lambda x: shapiro(x)[1], 'WeCM': lambda x: shapiro(x)[1], 'IDe': lambda x: shapiro(x)[1], 'TP': lambda x: shapiro(x)[1]})
shapiro_count = shapiro_test.map(lambda x: x < 0.05).sum()
print(shapiro_count)


# Count the occurrences of "Clockwise" and "CounterClockwise" in the "CircleDirection" column
clockwise_count = (data_art['CircleDirection'] == 'Clockwise').sum()
counterclockwise_count = (data_art['CircleDirection'] == 'CounterClockwise').sum()

#print(f"Number of 'Clockwise' rows: {clockwise_count}")
#print(f"Number of 'CounterClockwise' rows: {counterclockwise_count}")

#----------
#For aligned ranks transformation (ART):
#1. Take the data in the long format from the step 9 above

# data_art

#2. Create a bunch of files for each dependent variable (the first column is ID followed by the columns that represent conditions, the last column in each file should contain values of one dependent variable)
factors = [
    'Movement',
    'ReferenceFrame',
    #'TargetSize',
]
dependent_variable = 'DeclineDiff'
dependent_names = {
    'Success': 'Success',
    'MT': 'Movement Time',
    'WeCM': 'Effective Width',
    'IDe': 'Effective ID',
    'TP': 'Throughput',
    'SDx': 'Accuracy',
    'DeclineDiff': 'Decline',
    'RelativeTargetYaw': 'Relative Target Yaw',
}
dependent_measurements = {
    'Success': 'fraction',
    'MT': 'sec',
    'WeCM': 'cm',
    'IDe': 'bits',
    'TP': 'bits/sec',
    'SDx': 'm',
    'DeclineDiff': 'm',
    'RelativeTargetYaw': 'degrees',
}
refFrameOrder = ['PalmReferenced','PalmWORotation','PathReferenced']
refFrameTicks = ['Palm','PalmWOR','Path']
movementOrder = ['Standing','Walking','Circle']
movementTicks = ['Standing','Linear','Circular']
targetSizeOrder = ['0.02','0.03','0.04','0.05']
targetSizeTicks = ['2cm','3cm','4cm','5cm']
order = refFrameOrder if factors[0] == 'ReferenceFrame' else movementOrder if factors[0] == 'Movement' else targetSizeOrder
ticks = refFrameTicks if factors[0] == 'ReferenceFrame' else movementTicks if factors[0] == 'Movement' else targetSizeTicks

tp_dependent = data_art[['ParticipantID', 'Movement', 'ReferenceFrame', 'TargetSize', dependent_variable]].copy()
tp_dependent["Movement"] = tp_dependent["Movement"].astype('category')
tp_dependent["ReferenceFrame"] = tp_dependent["ReferenceFrame"].astype('category')
tp_dependent["TargetSize"] = tp_dependent["TargetSize"].astype('category')
tp_dependent["ParticipantID"] = tp_dependent["ParticipantID"].apply(str)
tp_dependent["ParticipantID"] = tp_dependent["ParticipantID"].astype('category')

import pingouin as pg
spher, _, chisq, dof, pval = pg.sphericity(tp_dependent, dv=dependent_variable,
                                           subject='ParticipantID',
                                           within=['TargetSize'])
print("Sphericity test TargetSize: ", spher)
spher, _, chisq, dof, pval = pg.sphericity(tp_dependent, dv=dependent_variable,
                                             subject='ParticipantID',
                                             within=['Movement'])
print("Sphericity test Movement: ", spher)
spher, _, chisq, dof, pval = pg.sphericity(tp_dependent, dv=dependent_variable,
                                                subject='ParticipantID',
                                                within=['ReferenceFrame'])
print("Sphericity test ReferenceFrame: ", spher)

import rpy2.robjects.packages as rpackages
import rpy2.robjects as ro
from rpy2.robjects import pandas2ri
ARTool = rpackages.importr('ARTool')

with (ro.default_converter + pandas2ri.converter).context():
    r_from_pd_df = ro.conversion.get_conversion().py2rpy(tp_dependent)

#3. Use ARTool to transform data taking one file by one (https://depts.washington.edu/acelab/proj/art/). Don't forget to tick 'Want contrasts' option to conduct post-hoc tests later on. This means that you'll need to 'Align and Rank' that many times as many contrast you want, e.g. 3 for 2 independent variables, 7 for 3, etc.
#print(":".join(factors))
ro.r('''
    f <- function(data) {
        m <- art(''' + dependent_variable + ''' ~ Movement*ReferenceFrame*TargetSize +  Error(ParticipantID), data=data)
        con <- art.con(m, "'''+":".join(factors)+'''", adjust="bonferroni")
        # m$aligned.ranks
        # summary(m)
        result <- anova(m)
        #print(names(m))
        #print(slotNames(m))
        #print(m$aligned.ranks)
        print(result)
        print(con)
        as.data.frame(summary(con))[c('contrast','p.value')]
    }
''')
f = ro.globalenv['f']
ro_con= f(r_from_pd_df)
with (ro.default_converter + pandas2ri.converter).context():
    df_con = ro.conversion.rpy2py(ro_con)

df_con["contrast"] = df_con["contrast"].str.replace("TargetSize", "")
df_con["contrast"] = df_con["contrast"].str.split("-", n=1, expand=False)
if len(factors) == 1:
    df_con["contrast"] = df_con["contrast"].apply(lambda x: (x[0].strip(), x[1].strip()))
else:
    df_con["contrast"] = df_con["contrast"].apply(lambda x: (tuple(x[0].strip().split(",")), tuple(x[1].strip().split(","))))

df_table = df_con.copy()
df_con = df_con[df_con["p.value"] < 0.05]
df_table[["contrast 1", "contrast 2"]] = pd.DataFrame(df_table['contrast'].to_list(), index=df_table.index)
df_table = df_table.drop(['contrast'], axis=1)
df_table[["contrast1 factor1", "contrast1 factor2"]] = pd.DataFrame(df_table['contrast 1'].to_list(), index=df_table.index)
df_table[["contrast2 factor1", "contrast2 factor2"]] = pd.DataFrame(df_table['contrast 2'].to_list(), index=df_table.index)
df_table = df_table.drop(['contrast 1', 'contrast 2'], axis=1)
df_table = df_table[['contrast1 factor1', 'contrast1 factor2', 'contrast2 factor1', 'contrast2 factor2', 'p.value']]
df_table = df_table.rename(columns={'p.value': 'p-value', 'contrast1 factor1': 'Factor-pair 1 (1)', 'contrast1 factor2': 'Factor-pair 1 (2)', 'contrast2 factor1': 'Factor-pair 2 (1)', 'contrast2 factor2': 'Factor-pair 2 (2)'})
df_table['p-value'] = df_table['p-value'].apply(lambda x: str(x)[:5] if x >= 0.05 else str(x)[:5] + " \cellcolor[HTML]{C0C0C0}" if x >= 0.001 else "<0.001 \cellcolor[HTML]{C0C0C0}" )
print(df_table.to_latex(index=False))
#print(df_con["contrast"].to_list())
grouped_for_stats = grouped_for_stats.groupby(factors).agg({'Success': 'mean', 'Success_STD': 'std', 'MT': 'mean', 'MT_STD': 'std', 'WeCM': 'mean', 'WeCM_STD': 'std', 'IDe': 'mean', 'IDe_STD': 'std', 'TP': 'mean', 'TP_STD': 'std'})
new_size = len(grouped_for_stats)
print(grouped_for_stats.to_string())

sns.set_theme(style="whitegrid")
ax = sns.boxplot(
    data=data_art, 
    x=factors[0], 
    y=dependent_variable,
    order=order,
    hue=factors[1] if len(factors) == 2 else None,
    hue_order=None if len(factors) == 1 else refFrameOrder if factors[1] == 'ReferenceFrame' else movementOrder if factors[1] == 'Movement' else targetSizeOrder,
    whis=(0, 100),
    showmeans=True,
    meanprops=dict(marker='x', markerfacecolor='black', markeredgecolor='black')
)
ax.set_xticklabels(ticks)
ax.set_ylabel(dependent_names[dependent_variable] + ' ('+dependent_measurements[dependent_variable]+')')
if len(factors) == 2:
    ticks = refFrameTicks if factors[1] == 'ReferenceFrame' else movementTicks if factors[1] == 'Movement' else targetSizeTicks
    for t in ticks:
        ax.legend_.texts[ticks.index(t)].set_text(t)
    ax.legend(bbox_to_anchor=(0.1, 1))
def pairs(x):
    return [(a, b) for idx, a in enumerate(x) for b in x[idx + 1:]]

box_pairs1 = pairs(data_art[factors[0]].unique())
if len(factors) == 1:
    box_pairs = box_pairs1
else:
    box_pairs = []
    for x1 in data_art[factors[0]].unique():
        for x2 in data_art[factors[1]].unique():
            box_pairs.append((x1, x2))
    box_pairs = pairs(box_pairs)

#print(box_pairs)

""" add_stat_annotation(
    ax,
    data=data_art,
    x=factors[0],
    y=dependent_variable,
    hue=factors[1] if len(factors) == 2 else None,
    hue_order=None if len(factors) == 1 else refFrameOrder if factors[1] == 'ReferenceFrame' else movementOrder if factors[1] == 'Movement' else targetSizeOrder,
    box_pairs = df_con["contrast"],
    order=order,
    text_format='star', 
    loc='inside',
    perform_stat_test=False, 
    pvalues=df_con["p.value"],
    verbose=0,
    test=None,
    line_offset_to_box=0.001 if len(factors) == 2 else None, 
    line_offset=0.0001 if len(factors) == 2 else None, 
    line_height=0.005 if len(factors) == 2 else 0.02, 
    text_offset=-7 if len(factors) == 2 else 1
) """
fig = plt.gcf()
if len(factors) == 1:
    fig.set_size_inches(3, 3.5)
else:
    fig.set_size_inches(6, 4)
plt.subplots_adjust(left=0.25, right=0.9, top=0.95, bottom=0.15)
fig.savefig(dependent_variable + '_'+factors[0] +'_'+(factors[1] if len(factors) == 2 else "")+'.png', dpi=100)
plt.show()

#4. Rename columns as such: ART(Effort) for Movement -> Effort_M, ART(Effort) for Movement*ReferenceFrame-> Effort_MxR, ART-C(Effort) for Movement -> Effort-C_M, etc.
#5. Combine all output files (only relevant columns) incl. contrasts into one file in long format
#6. Convert data to wide format again but now with the following parameters: Identifier = ParticpantID, Index Vars = all conditions
#Data Analysis
#1. Check the normality of all dependent variables using the Shapiro-Wilk test. Remember that for within-subject designs the normality should be checked independently for each condition
#2. Check out the skewness. Depending on it, decide which transformation to apply, if any (https://rpubs.com/frasermyers/627589)