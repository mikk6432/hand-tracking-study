import pandas as pd
import sys
import seaborn as sns
import matplotlib.pyplot as plt
import numpy as np
from statannot import add_stat_annotation
from scipy.stats import friedmanchisquare

new_data = pd.read_csv(sys.argv[1], delimiter=';')

new_data['Palm7'] = new_data["Please arrange the following reference frames in order of preference (the higher in the list the better):"].apply(lambda x: x.split(';').index('Palm') + 1)
new_data['Palm w/o Rotation7'] = new_data["Please arrange the following reference frames in order of preference (the higher in the list the better):"].apply(lambda x: x.split(';').index('Palm w/o Rotation') + 1)
new_data['Path7'] = new_data["Please arrange the following reference frames in order of preference (the higher in the list the better):"].apply(lambda x: x.split(';').index('Path') + 1)
print(new_data)

dependent_variable = "Preference"

data_df = pd.DataFrame()
for i in range(len(new_data)):
    row = new_data.iloc[i]
    data_df = data_df._append({
        "Reference Frame": "Palm",
        "Mental Demand": row["Palm"],
        "Physical Demand": row["Palm2"],
        "Temporal Demand": row["Palm3"],
        "Performance": row["Palm4"],
        "Effort": row["Palm5"],
        "Frustration": row["Palm6"],
        "Preference": row["Palm7"]
    }, ignore_index=True)
    data_df = data_df._append({
        "Reference Frame": "Palm w/o Rotation",
        "Mental Demand": row["Palm w/o Rotation"],
        "Physical Demand": row["Palm w/o Rotation2"],
        "Temporal Demand": row["Palm w/o Rotation3"],
        "Performance": row["Palm w/o Rotation4"],
        "Effort": row["Palm w/o Rotation5"],
        "Frustration": row["Palm w/o Rotation6"],
        "Preference": row["Palm w/o Rotation7"]
    }, ignore_index=True)
    data_df = data_df._append({
        "Reference Frame": "Path",
        "Mental Demand": row["Path"],
        "Physical Demand": row["Path2"],
        "Temporal Demand": row["Path3"],
        "Performance": row["Path4"],
        "Effort": row["Path5"],
        "Frustration": row["Path6"],
        "Preference": row["Path7"]
    }, ignore_index=True)

for i in range(1, 8):
    palm = "Palm" + (str(i) if i != 1 else "")
    palmwo = "Palm w/o Rotation" + (str(i) if i != 1 else "")
    path = "Path" + (str(i) if i != 1 else "")
    res = friedmanchisquare(new_data[palm], new_data[palmwo], new_data[path])
    print(res)

sns.set_theme(style="whitegrid")
ax = sns.boxplot(
    data=data_df, 
    x="Reference Frame", 
    y=dependent_variable,
    order=["Palm", "Palm w/o Rotation", "Path"],
    whis=(0, 100),
    showmeans=True,
    meanprops=dict(marker='x', markerfacecolor='black', markeredgecolor='black')
)
refFrameTicks = ['Palm','PalmWOR','Path']
ax.set_xticklabels(refFrameTicks)

add_stat_annotation(
    ax,
    data=data_df,
    x="Reference Frame",
    y=dependent_variable,
    box_pairs = [
        ("Palm", "Palm w/o Rotation"), 
        #("Palm", "Path"), 
        #("Palm w/o Rotation", "Path")
    ],
    order=["Palm", "Palm w/o Rotation", "Path"],
    text_format='star', 
    loc='inside',
    test='Wilcoxon',
    line_offset=0.1,
    line_height=0.02, 
    text_offset=-7
)
fig = plt.gcf()
fig.set_size_inches(3, 3.5)
plt.subplots_adjust(left=0.25, right=0.9, top=0.95, bottom=0.15)
fig.savefig(dependent_variable + '_ReferenceFrame'+'.png', dpi=100)
plt.show()