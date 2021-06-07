from abc import ABC
import os
import tempfile
import mmap
import numpy as np
import struct
import uuid
from typing import Tuple


class BaseSharedMemory(ABC):
    DIRECTORY = "ml-agents"

    def __init__(self, file_name: str, create_file: bool = False, size: int = 0):
        """
        Creates a bare bone shared memory wrapper that connects or creates a shared
        memory file.
        :string file_name: The name of the file used for shared memory
        :bool create_file: If true, the file will be created (any existing file with
        that name will be deleted).
        :int size: When creating the file, specifies its length in bytes
        """
        directory = os.path.join(tempfile.gettempdir(), self.DIRECTORY)
        if not os.path.exists(directory):
            os.makedirs(directory)
        file_path = os.path.join(directory, file_name)
        if create_file:
            if os.path.exists(file_path):
                os.remove(file_path)
            data = bytearray(size)
            with open(file_path, "w+b") as f:
                f.write(data)
        with open(file_path, "r+b") as f:
            # memory-map the file, size 0 means whole file
            self.accessor = mmap.mmap(f.fileno(), 0)
        self._file_path = file_path

    def get_int(self, offset: int) -> Tuple[int, int]:
        """
        Retrieves an integer from the shared memory
        :int offset: Where in the shared memory to retrieve the value
        :return: A tuple containing the value and the new offset
        """
        return struct.unpack_from("<i", self.accessor, offset)[0], offset + 4

    def get_float(self, offset: int) -> Tuple[float, int]:
        """
        Retrieves a float from the shared memory
        :int offset: Where in the shared memory to retrieve the value
        :return: A tuple containing the value and the new offset
        """
        return struct.unpack_from("<f", self.accessor, offset)[0], offset + 4

    def get_bool(self, offset: int) -> Tuple[bool, int]:
        """
        Retrieves a boolean from the shared memory
        :int offset: Where in the shared memory to retrieve the value
        :return: A tuple containing the value and the new offset
        """
        return struct.unpack_from("<?", self.accessor, offset)[0], offset + 1

    def get_string(self, offset: int) -> Tuple[str, int]:
        """
        Retrieves a string from the shared memory. The representation is an
        unsigned 8 bits value for the length followed by the bytes of the string
        in ASCII format.
        :int offset: Where in the shared memory to retrieve the value
        :return: A tuple containing the value and the new offset
        """
        string_len = struct.unpack_from("<B", self.accessor, offset)[0]
        byte_array = bytes(self.accessor[offset + 1 : offset + string_len + 1])
        result = byte_array.decode("ascii")
        return result, offset + string_len + 1

    def get_ndarray(
        self, offset: int, shape: Tuple[int, ...], t: np.dtype
    ) -> np.ndarray:
        """
        Retrieves a numpy array from the shared memory.
        :int offset: Where in the shared memory to retrieve the data
        :param shape: A tuple containing the shape of the expected array
        :param t: The dtype of the data to retrieve.
        :return: A new numpy array with the data from the shared memory
        """
        count = np.prod(shape)
        return np.frombuffer(
            buffer=self.accessor, dtype=t, count=count, offset=offset
        ).reshape(shape)

    def get_uuid(self, offset: int) -> Tuple[uuid.UUID, int]:
        """
        Retrieves a UUID from the shared memory
        :int offset: Where in the shared memory to retrieve the value
        :return: A tuple containing the value and the new offset
        """
        return uuid.UUID(bytes_le=self.accessor[offset : offset + 16]), offset + 16

    def set_int(self, offset: int, value: int) -> int:
        """
        Sets an integer into the shared memory.
        :int offset: The offset where to write the value
        :param value: The value to write
        :return: The new offset after writting the data
        """
        struct.pack_into("<i", self.accessor, offset, value)
        return offset + 4

    def set_float(self, offset: int, value: float) -> int:
        """
        Sets a float into the shared memory.
        :int offset: The offset where to write the value
        :param value: The value to write
        :return: The new offset after writting the data
        """
        struct.pack_into("<f", self.accessor, offset, value)
        return offset + 4

    def set_bool(self, offset: int, value: bool) -> int:
        """
        Sets a boolean into the shared memory.
        :int offset: The offset where to write the value
        :param value: The value to write
        :return: The new offset after writting the data
        """
        struct.pack_into("<?", self.accessor, offset, value)
        return offset + 1

    def set_string(self, offset: int, value: str) -> int:
        """
        Sets a string into the shared memory.
        :int offset: The offset where to write the value
        :param value: The value to write
        :return: The new offset after writting the data
        """
        string_len = len(value)
        struct.pack_into("<B", self.accessor, offset, string_len)
        offset += 1
        self.accessor[offset : offset + string_len] = value.encode("ascii")
        return offset + string_len

    def set_uuid(self, offset: int, value: uuid.UUID) -> int:
        """
        Sets a UUID into the shared memory.
        :int offset: The offset where to write the value
        :param value: The value to write
        :return: The new offset after writting the data
        """
        self.accessor[offset : offset + 16] = value.bytes_le
        return offset + 16

    def set_ndarray(self, offset: int, data: np.ndarray) -> None:
        """
        Sets a numpy array into the shared memory.
        :int offset: The offset where to write the value
        :param data: The value to write
        """
        bytes_data = data.tobytes()
        self.accessor[offset : offset + len(bytes_data)] = bytes_data

    def close(self) -> None:
        """
        Closes the shared memory reader and writter. This will not delete the file but
        remove access to it.
        """
        if self.accessor is not None:
            self.accessor.close()
            self.accessor = None  # type: ignore

    def delete(self) -> None:
        """
        Closes and deletes the shared memory file.
        """
        self.close()
        try:
            os.remove(self._file_path)
        except BaseException:
            pass
