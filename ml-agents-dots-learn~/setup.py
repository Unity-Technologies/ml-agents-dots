from setuptools import setup, find_packages

setup(
    name="mlagents_dots_learn",
    version="0.2.0-preview",
    description="Unity Machine Learning Agents Learner for DOTS",
    url="https://github.com/Unity-Technologies/ml-agents-dots",
    author="Unity Technologies",
    author_email="ML-Agents@unity3d.com",
    classifiers=[
        "Intended Audience :: Developers",
        "Topic :: Scientific/Engineering :: Artificial Intelligence",
        "Programming Language :: Python :: 3.6",
        "Programming Language :: Python :: 3.7",
    ],
    packages=find_packages(
        exclude=["*.tests", "*.tests.*", "tests.*", "tests", "examples"]
    ),
    zip_safe=False,
    install_requires=["mlagents_dots_envs>=0.2.0-preview", "mlagents>=0.25.0"],
    python_requires=">=3.6",
    entry_points={
        "console_scripts": ["mlagents-dots-learn=mlagents_dots_learn.dots_learn:main"]
    },
)
