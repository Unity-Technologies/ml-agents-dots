import os
import sys
from setuptools import setup, find_packages
from setuptools.command.install import install
import mlagents_dots_envs

setup(
    name="mlagents_dots_envs",
    version="0.2.0-preview",
    description="Unity Machine Learning Agents Interface for DOTS",
    url="https://github.com/Unity-Technologies/ml-agents-dots",
    author="Unity Technologies",
    author_email="ML-Agents@unity3d.com",
    classifiers=[
        "Intended Audience :: Developers",
        "Topic :: Scientific/Engineering :: Artificial Intelligence",
        "Programming Language :: Python :: 3.6",
        "Programming Language :: Python :: 3.7",
    ],
    packages=find_packages(exclude=["*.tests", "*.tests.*", "tests.*", "tests", "examples"]),
    zip_safe=False,
    install_requires=[ "mlagents_envs>=0.14.1"],
    python_requires=">=3.6",
)
