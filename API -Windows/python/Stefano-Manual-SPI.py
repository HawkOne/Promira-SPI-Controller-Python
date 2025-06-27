

# Manual - SPI 

"""
Configure - Configure the SPI master or slave interface.

-Arguments
 channel	handle of the channel
 mode	PS_SPI_MODE_0, PS_SPI_MODE_1, PS_SPI_MODE_2 or PS_SPI_MODE_3
 bitorder	PS_SPI_BITORDER_MSB or PS_SPI_BITORDER_LSB
 ss_polarity	bitmask of the polarity of the slave select signals.
 The ss_polarity option is a bitmask that indicates whether each SS line is active high or active low.
 For example, setting ss_polarity to 0x05 would mean that SS3 and SS1 are active high and all others are active low.

 -Return Value
 A status code is returned with PS_APP_OK on success.
"""

SlaveBitmask = 0x00 #(All slaves are active low)

a = ps_spi_configure(HANDLER_South, PS_SPI_MODE_0, PS_SPI_BITORDER_MSB, SlaveBitmask );
if (a == PS_APP_OK ) : print(f"Configure Successful")

"""
Configure Delays (ps_spi_configure_delays)

int ps_spi_configure_delays (PromiraChannelHandle channel, u08 word_delay);

Configure the delays.
Arguments
channel	handle of the channel
word_delay	The number of clock cycles between data words.
Return Value

A status code is returned with PS_APP_OK on success.
Specific Error Codes
None.
Details

The word_delay parameter is a user-definable delay between words. It can be 0 or greater than 1 and no gap is included after the last word.

"""

a = ps_spi_configure_delays (HANDLER_South, SPI_Word_Delay);
if (a == PS_APP_OK ) : print(f"Word Delay set Successful")

"""

Enable SS Lines (ps_spi_enable_ss)

int ps_spi_enable_ss (PromiraChannelHandle channel, u08 ss_enable);

Enable select SS lines and disable GPIO lines.
Arguments
channel	handle of the channel
ss_enable	A bitmask based on the 8 SS lines where 1 corresponds to enable and 0 to disable.
Return Value

This function returns the actual SS mask set.
Specific Error Codes
None.
Details

ss_enable is to enable which pins are configured to ss line instead of GPIO. The least significant bit is SS0.

"""

SPI_CS_Enable_Mask = 0x01

a = ps_spi_enable_ss (HANDLER_South, SPI_CS_Enable_Mask);
if (a == SPI_CS_Enable_Mask ) : print(f"SS Enable Mask set Successful -> { a }")



"""

Enable Master Outputs (ps_queue_spi_oe)

int ps_queue_spi_oe (PromiraQueueHandle queue, u08 oe);

Enable/disable the outputs.
Arguments
queue	handle of the queue
oe	0 to disable the outputs, and 1 to enable
Return Value

A status code is returned with PS_APP_OK on success.
Specific Error Codes
None.
Details

This function enables and disables the outputs on the Promira.
"""
#SPI_QUEUE_HANDLER = ?
#
#a = ps_queue_spi_oe (SPI_QUEUE_HANDLER, 1);
#if (a == PS_APP_OK ) : print(f"Enable Master Outputs set Successful -> { a }")


"""
Queue Slave Select Signals (ps_queue_spi_ss)

int ps_queue_spi_ss (PromiraQueueHandle queue, u08 ss_assert);

Queue the assertion and de-assertion of slave select signals.
Arguments
queue	handle of the queue
ss_assert	bitmask where 1 asserts the slave select and 0 de-asserts
Return Value

A status code is returned with PS_APP_OK on success.
Specific Error Codes
None.
Details

The least significant bit is SS0.

The outputs should be enabled using ps_queue_spi_oe before calling this function.
"""

#SPI_QUEUE_HANDLER = ?
#
#a = ps_queue_spi_ss (SPI_QUEUE_HANDLER, 0x01);
#if (a == PS_APP_OK ) : print(f"Enable Master Outputs set Successful -> { a }")


"""

5.7.3 SPI Master


Set Bitrate (ps_spi_bitrate)

int ps_spi_bitrate (PromiraChannelHandle channel, int bitrate_khz);

Set the SPI bitrate in kilohertz.
Arguments
channel	handle of the channel
bitrate_khz	the requested bitrate in khz
Return Value

This function returns the actual bitrate set.
Specific Error Codes
None.
Details

The power-on default bitrate is 1000 kHz (1 MHz).

Only certain discrete bitrates are supported by the SPI subsystem. As such, this actual bitrate set will be less than or equal to the requested bitrate unless the requested value is less than 31 kHz, in which case the SPI subsystem will default to 31 kHz.

If bitrate_khz is 0, the function will return the bitrate presently set on the Promira application and the bitrate will be left unmodified.
"""



print(f"SPI Frequency at turn on is { ps_spi_bitrate (HANDLER_South, SPI_Frequency)} (Standard)")

a = ps_spi_bitrate (HANDLER_South, SPI_Frequency);
if (a == SPI_Frequency ) : print(f"SPI Frequency set Successfully -> { a }")


"""
Queue a Delay in Cycles (ps_queue_spi_delay_cycles)

int ps_queue_spi_delay_cycles (PromiraQueueHandle queue, u32 cycles);

Queue a delay value on the bus in units of clock cycles.
Arguments
queue	handle of the queue
cycles	cycles of delay to add to the outbound shift
Return Value

A status code is returned with PS_APP_OK on success.
Specific Error Codes
None.
Details

Queues cycles amount of delay on the bus. These are in units of clock cycles as set with ps_spi_bitrate.

Actual number of cycles queued is returned when collecting a response using ps_collect_resp.
"""

a = ps_queue_spi_delay_cycles (HANDLER_South, SPI_Word_Delay);
if (a == PS_APP_OK ) : print(f"SPI Word Delay set Successfully -> { SPI_Word_Delay }")


"""
Queue SPI Master Write (ps_queue_spi_write)

int ps_queue_spi_write (PromiraQueueHandle queue, PromiraSpiIOMode io, u08 word_size, u32 out_num_words, const u08 * data_out);

Queue a command that writes a stream of words to the downstream SPI slave device.
Arguments
queue	handle of the queue
io	IO mode flag as defined in table 12
word_size	number of bits for a word; between 2 and 32
out_num_words	number of words to send
data_out	pointer to the array of words to send
Return Value

A status code is returned with PS_APP_OK on success.
Specific Error Codes
None.
Details

This function queues the command, it will be executed when the function ps_queue_submit or ps_queue_async_submit is called.

The outputs should be enabled using ps_queue_spi_oe before calling this function.

data_out is a buffer containing a bitwise concatenation of words to be sent out. For instance, when word size is 4 and words are 0x1 0x2 0x3 0x4 0x5, then data in a buffer looks like 0x12, 0x34, 0x50. The size of data_out should be equal to or bigger than (word_size * out_num_words + 7) / 8.

The actual data read and the number of words read will be returned with the function ps_collect_resp and ps_collect_spi_read when collecting.
Table 12 : SPI IO Modes
PS_SPI_IO_STANDARD	Standard, full-duplux SPI.
PS_SPI_IO_DUAL	Dual mode SPI.
PS_SPI_IO_QUAD	Quad mode SPI.
"""

SPI_Word_Size = 32
SPI_Write_Number_of_Words = 1

Stringa_Da_Mandare = "CIAO MONDO!"


a = ps_queue_spi_write (HANDLER_South, PS_SPI_IO_STANDARD, SPI_Word_Size, SPI_Write_Number_of_Words, Stringa_Da_Mandare);
if (a == PS_APP_OK ) : print(f"Successfully QUEUED A WRITE-> { Stringa_Da_Mandare }")


#This function queues the command, it will be executed when the function ps_queue_submit or ps_queue_async_submit is called.
#
#The outputs should be enabled using ps_queue_spi_oe before calling this function.
#
#data_out is a buffer containing a bitwise concatenation of words to be sent out. For instance, when word size is 4 and words are 0x1 0x2 0x3 0x4 0x5, then data in a buffer looks like 0x12, 0x34, 0x50. The size of data_out should be equal to or bigger than (word_size * out_num_words + 7) / 8.
#
#The actual data read and the number of words read will be returned with the function ps_collect_resp and ps_collect_spi_read when collecting.

"""
ps_queue_spi_oe()
ps_queue_submit()

(PromiraCollectHandle return, u08 queue_type) = ps_queue_submit(PromiraQueueHandle queue, PromiraChannelHandle channel, u08 ctrl_id)


#ps_queue_async_submit

# ///actual data read
#ps_collect_resp

# ////number of words read
#ps_collect_spi_read

"""


"""
Queue SPI Master Write Word (ps_queue_spi_write_word)

int ps_queue_spi_write_word ( PromiraQueueHandle queue, PromiraSpiIOMode io, u08 word_size, u32 out_num_words, u32 word);

Queue a command that writes a stream of the same word to the downstream SPI slave device
Arguments
queue	handle of the queue
io	IO mode flag as defined in table 12
word_size	number of bits for a word; between 2 and 32
out_num_words	number of words to send
word	value of the word to queue
Return Value

A status code is returned with PS_APP_OK on success.
Specific Error Codes
None.
Details

Queues out_num_words number of words to send and sets each word to the value of word.

The outputs should be enabled using ps_queue_spi_oe before calling this function.
"""





"""
Queue SPI Master Read (ps_queue_spi_read)

int ps_queue_spi_read (PromiraQueueHandle queue, PromiraSpiIOMode io, u08 word_size, u32 in_num_words);

Queue a command that performs an SPI read operation.
Arguments
queue	handle of the queue
io	IO mode flag as defined in table 12
word_size	number of bits for a word; between 2 and 32
in_num_words	number of words to clock in
Return Value

A status code is returned with PS_APP_OK on success.
Specific Error Codes
None.
Details

This function queues the command, it will be executed when the function ps_queue_submit or ps_queue_async_submit is called.

The outputs should be enabled using ps_queue_spi_oe before calling this function.

When io is PS_SPI_IO_STANDARD, this function is equivalent to ps_queue_spi_write_word with word equal to 0. When io is PS_SPI_IO_DUAL or PS_SPI_IO_QUAD, the clock is generated and the data lines are set to inputs.

The actual data read and the number of words read will be returned with the function ps_collect_resp and ps_collect_spi_read when collecting.

"""









"""
Collect a Master Write/Read (ps_collect_spi_read)

int ps_collect_spi_read (PromiraCollectHandle collect, u08 * word_size, u32 in_num_bytes, u08 * data_in);

Collect the response of SPI master read.
Arguments
collect	handle of the collection
word_size	number of bits for a word received
in_num_bytes	number of bytes to receive
data_in	array into which the data read are returned
Return Value

This function returns the total number of bytes read from the slave.
Specific Error Codes
PS_APP_MISMATCHED_CMD	The type of response is not PS_SPI_CMD_READ.
Details

This function should be called right after the function ps_collect_resp returns PS_SPI_CMD_READ. Once the function ps_collect_resp is called again, then data received will be discarded. However this function can be called many times before the function ps_collect_resp is called.

data_in is returned with data containing a bitwise concatenation of words received. For instance, when word size is 4 and words received are 0x1 0x2 0x3 0x4 0x5, then data returned looks like 0x12, 0x34, 0x50.

"""
