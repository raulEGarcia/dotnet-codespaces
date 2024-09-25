using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace quNS
    {
    public class QMF
        {

        //---policy - to avoid issues of numeric data types & conversion - all long, int, or float will be double

        //---policy (logic) important - the functions will receive the direct numeric parms server, K etc
        //---there will be no check in these functions if the queue type is consistent with then
        //---the logic is if server = 1 then MM1X, if server>1 then MMSX, 
        //---if K=0 them MMS OR MM1 (MMX0) if K>0 then MMXK (MM1K OR MMSK)

        // BASE THE MODEL FORMULAS ON RECURSIVE LAMBDA FORMULAS FOR 1-SIMPLICITY AND 2-FLEXIBILITY
        // C# PRINCIPLES: 1- USE SHORT FUNCTIONS FOR PERFORMANCE 2-USE PROPERTIES FOR GLOBAL VARS
        // 3- USE FOREACH OVER AN ITERATOR (ARRAY WITH INT VALUES) ETC FOR BEST PERFORMANCE
        // 4-ALWAYS OVERRIDE TOSTRING() FOR CLASS ETC 5-LAMBDAS CAPTURE LAST VALUE IF EXTERNAL VAR
        // LAMBDA NOTATION: =>  EX: Func<Double,Double> P0 = (mu,lambda) => mu/lambda;
        // C# REFS: EFFECTIVE C# - WAGNER, C# IN DEPH - SKEET - LAMBDA CHAPTER - P 230

        //--- COMMON ERRORS TO WATCH: 
        // 1-double vs double, double vs Double, bool vs Boolean (use 1st)
        // 2-Dont use '= >'  NO space it is '=>'
        // 3-the last parm is the func return type
        // 4-it's Func NO Funct
        // 5 - L, LQ, Lw are calc in a certain order for each queue type and used several predefined funcs

        //public struct que
        //    {
        //    string queType;
        //    bool queSteady;
        //    string queRunId;
        //    Double lambda;
        //    Double mu;
        //    Double servers;
        //    Double n;
        //    Double K;
        //    }

        /// fields
        ///  quTypeOut - string array of 4 chars of queue type
        ///  quProbList - list of doubles of probabilities for each n
        ///  
        public static String [ ] quTypeOut = new String [ 4 ];
        const int lim = 100;
        public double [ ] probArray = new double [ lim ];
        public static List<double> quProbList = new List<double>() ; 
        public double quL, quLQ, quW, quWQ; 
        // <summary>
        /// Erro codes: 
        /// </summary>
        const double errorCode1 = -991; // error - arrive
        const double errorCode2 = -992;
        const double errorCode3 = -993; // error in prob calcs - passed queue type
        const double errorCode4 = -994; // error in prob calcs - passed queue type - LWQ method
        //--- TEMP - DEF OF FACTORIAL -LATER USE BIG INT

        /// <summary>
        /// Factorial function - fact
        /// </summary>
        /// <param name="n">Number to return it's factorial or n!</param>
        /// <returns></returns>
        public static double fact( double n )
            {
            double f = 1L; // must init to 1
            for ( int ix = 1; ix <= n; ix++ ) { f *= ix; }
            return f;
            }


        // (input parameters) => {statement;}
        // (input parameters) => expression


        /// <summary>
        /// These are the two fundametal fourmulas of Queing systems (the same when s=1)
        /// Steady state formula: (rho) rho = lambda / ( mu * servers ) - returns double
        /// </summary>

        public Func<double, double, double, double> rho = ( lambda, mu, servers ) => lambda / ( mu * servers ); //--traffic intensity: lambda/(mu * servers )  
        public Func<double, double, double> rho1 = ( lambda, mu ) => ( lambda / mu );  //--traffic intensity: lambda/mu if server is one

        //--- TEMPLATE FOR FUNCTION COMPOSITION
        //
        //Func<T3, T1> my_chain( Func<T2, T1> f1, Func<T3, T2> f2 )
        //    {
        //    return ( x => f2( f1( x ) ) );
        //    }

        /// <summary>
        /// Steady State of Queue true or false - ss0 - based on r - rho (must be <1 for steady) - returns boolean
        /// </summary>
        Func< double, bool > ss0 = ( r ) =>
            {
                if ( r < 1 ) { return true; } else { return false; }
            };


        /// <summary>
        /// Define steady state Warning Levels (ssWL) for p0 (rho) values close to 1 - returns string
        /// </summary>

        const String wl = "Steady State: Warning Level#";

        public Func< double, string > ssWL0 = ( r ) =>
        {
            if ( r < 1 && r < 0.90 ) { return ( wl + "0 - below 90% Capacity - " + r.ToString() ); } else { return ""; }
        };

        public Func< double, string > ssWL1 = ( r ) =>
        {
            if ( r < 1 && r >= 0.90 ) { return ( wl + "1 - on or over 90% Capacity" ); } else { return ""; }
        };

        public Func< double, string > ssWL2 = ( r ) =>
        {
        if ( r < 1 && r >= 0.95 ) { return ( wl + "2 - on or over 95% Capacity" ); } else { return ""; }
        };

        public Func< double, string > ssWL3 = ( r ) =>
        {
        if ( r < 1 && r >= 0.98 ) { return ( wl + "3 - on or over 98% Capacity" ); } else { return ""; }
        };

        public Func< double, string > ssWL4 = ( r ) =>
        {
        if ( r >= 1 ) { return ( "UN-" + wl + "4 - on or over 100% Capacity" ); } else { return ""; }
        };


        /// <summary>
        ///  The arrive avg rate - An - is lambda for infinity capacity always, else it's lambda until K then zero
        ///  These are the fundamental values for arrival/depart rates for popn = n where the queing formulas are derived from
        /// </summary>

        public Func< double, double, double, double > An = ( n, lambda, K ) => //--- DEPENDS ON n, not on servers
            {
            if ( n >= 0 && K == 0         ) { return lambda; } //--- infinite Capacity, same for M/M/1 and M/M/S - 9.12 AND 9.19
            if ( n >= 0 && K > 0 && n < K ) { return lambda; } //--- K - capacity is used - 9.26 AND 9.34 - M/M/1/K AND  M/M/S/K
            if ( n >= 0 && n >= K         ) { return 0; } //--- K - over capacity - no more arrivals -  - 9.26 AND 9.34
            else { return 0; } //--error 

            }; //--- VERIFIED AND ALL RELATED FIUNCTIONS - 12/17/13- REG SHAUM PROB HSU P280-286


        /// <summary>
        ///  The depart avg rate is mu for s=1, k=0; mu*n for n lt s and mu*s for n>servers
        /// </summary>

        public Func< double, double, double, double, double > Dn = ( n, mu, K, servers ) =>
        {
            if ( n >= 1 && servers==1 && K==0 ) { return mu; } //--- infinite Capacity and 1 server - fixed mu - M/M/1 -- 9.12
            if ( n >= 1 && servers > 0 && n <  servers && K == 0  ) { return mu * n; } //--- any K, n < servers then depart n*mu - 9.19  top - M/M/S
            if ( n >= 1 && servers > 0 && n <  servers && K > 0   ) { return mu * n; } //--- any K, n < servers then depart n*mu - 9.34  top - M/M/S/K
            if ( n >= 1 && servers > 0 && n >= servers && K == 0 )  { return mu * servers; } //--- any K, n < servers then depart n*mu - 9.19  bot - M/M/S
            if ( n >= 1 && servers > 0 && n >= servers && K > 0  )  { return mu * servers; } //--- any K, n < servers then depart n*mu - 9.34  bot - M/M/S/K
            else { return 0; } //--- error
        }; //--- VERIFIED AND ALL RELATED FIUNCTIONS - 12/17/13- REG SHAUM PROB HSU P280-286


        /// <summary>
        /// quType a 4 string array to decompose string 'MMSK' where K=0 for infinite, 1 for finite capacity, S=1 ONE server etc
        /// Example quTypeIn maps "MM10" to quTypeOut = { "M", "M", "1", "0" } for M/M/1
        /// Example quTypeIn maps "MMSK" to quTypeOut = { "M", "M", "S", "K" } for M/M/S/S for S>1 servers with finite capacity K
        /// Codes M = Markovian, K=0 infinite capacity , else 1; S=1 for 1 server else S
        /// </summary>


        public Func<String, String [ ]> quTypeIn = ( type ) =>
           {
           String type1;
              type1 = type.Trim( );
              if ( type1.Length < 4 )
               {
               // some error - must be 4 chars
              
               return null;
               }
               type1 = type1.ToUpper( );
               String [ ] qu = new String[4];
               qu[ 0 ] = type1.Substring( 1, 1 );
               qu[ 1 ] = type1.Substring( 2, 1 );
               qu[ 2 ] = type1.Substring( 3, 1 );
               qu[ 3 ] = type1.Substring( 4, 1 );

               //-very valid values MMSK, MM1K, MMS0, MM10
               
               if ( qu[ 0 ] != "M" || ( qu[ 1 ] != "M" ) ) return null;
               if ( qu[ 2 ] != "1" || ( qu[ 2 ] != "S" ) ) return null;
               if ( qu[ 3 ] != "0" || ( qu[ 3 ] != "K" ) ) return null; 

               return qu;
           };


        /// <summary>
        /// Partial sum 1 to be used in queue Probability p0-pn formulas
        /// </summary>
        /// <param name="?">r=rho, population-n,K-capacity (0 if n/a), m-start for partial sums </param>
        /// <returns></returns>


        private static Func< double, double, double, double > pSum1 = ( r, servers, K  ) =>
        {
               
        double retVal = 0;
      

        if ( K == 0 )
            {
             for (double ix=0; ix <= servers - 1; ix++)
                {
                retVal += (servers * r) / fact( ix ); // 9.20 1stterm M/M/S
                }

            }
        else if  ( servers == 1 && K > 0 )
            {
            retVal = ( 1 - Math.Pow( r, K + 1 ) ) / ( 1 - r ); //-- 9.29 M/M/1/K the inverse since the sum is later inverted
            }
        else if ( servers > 1 && K > 0 )
        {
        for ( double ix = 0; ix <= servers - 1; ix++ )
            {
            retVal += Math.Pow( servers * r, ix ) / fact( ix ); // 9.20 1stterm M/M/S
            }

            }

        return retVal;

        };

        /// <summary>
        /// Partial sum 2 - part of the prob sums
        /// rho, servers, capacity
        /// </summary>

        private static Func< double, double, double, double > P0 = ( r, servers, K ) =>
        {
            //--- orghanized into two sums, the answer is the inverse of these added
            double retVal = 0;
            double tmpSum = 0;

            double ps = 0; //---store a common term
            ps = Ps( servers ) ;


           // SPECIAL CASE - M/M/1

            if ( servers == 1 && K == 0 ) 
            {
            retVal = 1 - r;// ---note since server=1 then lambda/(server*mu) = lambda/server
            return retVal;
            }
    
        //--- sum 1

          
           double tmpSum1 = 0; // --- THE FORMULA 9.19 AND SIMILAR IS MADE LIKE 1/[SUM1+SUM2]
            
            if ( K == 0 && servers > 1 )
                {
              
                 tmpSum1 = pSum1( r, servers, K ); // - note index is servers//9.20 for M/M/S
                 

                }
            else if ( K > 0 && servers == 1 ) //K>0 - M/M/1/K 
                {
                tmpSum1 = ( 1 - r ) / ( 1 - Math.Pow( r, K + 1 ) ) ; //-- 9.27 for M/M/1/K
                }
      
        //-- sum 2

            double tmpSum2 = 0;
           
            if ( servers > 1 && K == 0 )
                {
                tmpSum2 = ps * Math.Pow( r, servers ) / ( 1 - r ); // 9.20 2ND TERM - M/M/S
                  
                }
            else if ( servers > 1 && K > 1) 
                {
                //--- take non-loop calcs out 
                double tmp1rs = 0;
                tmp1rs = Math.Pow( r, servers );

                double tmp2rk = 0;
                tmp2rk = ( 1 - Math.Pow( r, K - servers + 1 ) ) / ( 1 - r ) ; // right term of 9.35

                tmpSum2 = ps * tmp1rs * tmp2rk; // 9.35 for M/M/S/K
                    
                }
            else { tmpSum2 = 0; } // no such 2nd term for K>0 and s=1

            //--- for all cases we have the inverse of the sum now

            tmpSum = tmpSum1 + tmpSum2;

            if ( tmpSum != 0 ) retVal = 1 / tmpSum; 
        // resumt is the inverse of sum: sum1 and sum2

            return retVal;

        };

        /// <summary>
        /// A frequent sub-formula factor; servers to the power of server over factorial of server
        /// parm - servers - s^s/s!  
        /// </summary>

        private static Func< double, double > Ps = ( servers ) =>
            {   
                //--- this function grows exponentially - and it's log is almost a straight line!
                //--- thus there are critical growth values such as 15 in 20 servers etc.
                double retVal = 0;
                double factServer;
              

                if ( servers <= 1 ) 
                    { 
                    retVal = 1; 
                    }
                else
                    {
                    factServer = Convert.ToDouble( fact( servers ) );
                    retVal = Math.Pow( servers, servers ) / factServer; // s^s/s! - a common term 9.35, 9.20, 9.22
                    }

                return retVal;

            };


        private Func<double, double, double> Pn0 = ( r, n ) =>
           {
               double retVal=0;
               retVal = ( 1 - r ) * Math.Pow( r, n ); // -- 9.14
               return retVal;
           };

        Func< double, double, double, double, double > Pn = ( r, n, servers, K ) =>
            {
            double retVal = 0;
            double p0 = 0;
            p0 = P0( r, servers, K );
         

            if ( servers == 1 && K == 0 ) // //-- special case M/M/1
                 {
                 retVal = ( 1 - r ) * Math.Pow( r, n );  //--9.14
                 // ---note since server=1 then lambda/(server*mu) = lambda/server
                 return retVal;
               
                }

            if( n < servers )
                {
                retVal = p0 * Math.Pow( servers * r, n ) / fact( n ) ; //--9.36, 9.21 - 1st n<s
                }
            else
                {
                retVal = p0 * Ps( servers ) * Math.Pow( r, n ); //--9.36, 9.21 - 2nd n>=s
                }
            
                return retVal;

            };

        //--- below Pn1 etc are common values/formulas for quPROBn (the pn - or Pn in Shaum)


        private Func<double, double, double, double, double> Pn1 = ( r, n, p0, servers ) =>
        {
            double retVal = 0;
           
            retVal = ( Math.Pow( r*servers, n ) / fact( n ) ) * p0; // 9.21 - top n<s
           
            return retVal;

        };

        private Func<double, double, double, double, double> Pn2 = ( r, n, p0, servers ) =>
        {
            double retVal = 0;

            retVal = Math.Pow( r, n ) * Ps( servers ) * p0; // 9.21 - bottom n>s

            return retVal;

        };


        private Func<double, double, double, double, double> Pn1K = ( r, n, servers, K ) =>
        {
            double retVal = 0;
            double p0 = 0;
            p0 = P0( r, servers, K );
            retVal =  ( 1 - r ) * Math.Pow( r, n )/ ( 1 - Math.Pow( r, K+1 ) ); // 9.28, with n=K is Pk in 9.29-9.33
        // commpn value in L, Q etc

            return retVal;

        };


        /// <summary>
        /// main METHOD for queue Probability for all MM cases MMSK
        /// load public array quTypeOut before this call
        /// K=0 case - INFINITE CAPACITY
        /// </summary>


        public double quPn( double n, double r, double servers, double K ) //--Pn in Shaum , p0..pn
        {

        //-- note: we can deduce the M/M/X/X TYPE FROM THE VALUES IN servers and K
        
            double retVal = 0;
            double p0 = 0;
            p0 = P0( r, servers, K );  //-- used below, implied n=0 1st prob term

            //--- only processes M/M/X/X QUEUES - MARKOVIAN

            if ( n == 0 ) return p0;

            //--- asume n > 0

            if ( servers == 1 && K == 0 ) //MM1
                {

                retVal = Pn0( r, n ); //-- 9.14
                return retVal;
                }
            
         

            if (  servers > 1 && n < servers && K == 0 ) //MMS0
                {

                retVal = Pn1( r, n, p0, servers ); // 9.21 - top n<s
                return retVal;
                }
 
            if ( servers > 1 && n >= servers ) // MMS
                {

                retVal = Pn2( r, n, p0, servers ); // 9.21 - bottom n>s
                return retVal;
                }


           if (  servers > 1 && n < servers && K >0 ) // MMSK
               {

               retVal = Pn2( r, n, p0, servers ); // 9.36 same as 9.21 - top n<s
               return retVal;
               }

           if ( servers > 1 && n >= servers && n <= K ) // MMSK
              {

              retVal = Pn2( r, n, p0, servers ); // 9.36 same as 9.21 - bottom s<=n<=K
              return retVal;
              }   

            return 0;

        }

       
       public void loadProbArray( String quType, double n, double r, double servers, double K )
            {

            //-- make sure it's zero
            for ( int ix = 0; ix < lim; ix++ ) { probArray [ ix ] = 0; }

            for ( int popn = 0; popn <= n; popn++ )
            {
              //load probbability for each value of population up to limit n
            if ( popn >= lim ) return; // for now dont exceed the array size
            probArray [ popn ] = quPn( n, r, servers, K );
           
            } //--- VERIFIED AND ALL RELATED FIUNCTIONS - 12/16/13- REG SHAUM PROB HSU P280-286



        }

       public bool checkQueueDataConsistency( String quType, double K, double servers )
           {

           const string msg1 = "Please correct inconsistent Queue TYPE and Queues DATA (servers or capacity):  \n\n";

           quTypeOut = quTypeIn( quType ); // --- LOAD DECOMPOSED QU TYPES
           //--- only processes M/M/X/X QUEUES - MARKOVIAN
           if ( quTypeOut [ 0 ] != "M" || quTypeOut [ 1 ] != "M" ) return false;

           //--- CHECK CONSIDETENCY OF NUMERIC DATA AND CODES

           if ( servers == 1 && quTypeOut [ 2 ] != "1" )
               {
               MessageBox.Show( msg1 + "The numbers of Servers is 1 - but the Queue TYPE is not M/M/1/X", "Data Consitency - Servers - Needs Update" );
               return false;
               }
           if ( servers > 1 && quTypeOut [ 2 ] != "S" )
               {
               MessageBox.Show( msg1 + "The numbers of Servers is >1 but the Queue TYPE is not M/M/S/X", "Data Consitency - Servers - Needs Update" );
               return false;
               }

           if ( K == 0 && quTypeOut [ 3 ] != "0" )
               {
               MessageBox.Show( msg1 + "The site Capacity (K) number is zero, but the Queue TYPE is not M/M/X/0", "Data Consitency - Capacity - Needs Update" );
               return false;
               }


           if ( K > 0 && quTypeOut [ 3 ] != "K" )
               {
               MessageBox.Show( msg1 + "The site Capacity (K) numbers >0, but the Queue TYPE is not M/M/X/K", "Data Consitency - Capacity - Needs Update" );
               return false;
               }

           return true;

           }



       public bool loadLWQ( double lambda, double mu, double n, double K, double servers )
        { 
            
           /// will load into:  quL, quLQ, quW, quWQ  for each case per quType
           /// 
            quL = 0; quLQ = 0; quW = 0; quWQ = 0;
           
           // temp vals 
            double tmpNum = 0;
            double tmpDen = 0;

            double r = 0;
            r = rho( lambda, mu, servers ); // --traffic intensity: lambda/mu*servers
            
            double r1 = 0;
            r1 = rho1( lambda, mu ); //--traffic intensity: lambda/mu if server is one
            //--- need P0, PK
            double p0=0;
            p0 = P0( r, servers, K );
         
            double ps=0;
            ps = Ps( servers );

            double pk = 0;
            pk = Pn1K( r, K, servers, K ); // where n=k   

           //--- NOTE - THESE CALCS L,LQ ETC MUST BE DONE IN A CERTAIN ORDER - VALUES MAY DEPEND ON PREV CALC VALUES 

           if ( servers == 1 && K == 0 ) // MM10 - 1 SERVER - UNLIM CAP (OR M/M/1)
               {
               quL = r1 / ( 1 - r1 ); //9.15

               quW = 1 / ( mu - lambda ); // 9.16

               quWQ = r1 * quW;  // 9.17

               quLQ = lambda * quWQ; // 9.18
        
               return true; // verified 12/16/13 - reg - shaum prob hsu
                
               }

           if ( servers > 1 && K == 0 ) // MMS0 - >1 SERVER - UNLIM CAP (OR M/M/S)
               {

               quL = r1 + ( p0 * ps ) * ( Math.Pow( r , servers + 1 ) / Math.Pow( ( 1 - r ), 2 ) );   // 9.22
               
               quLQ = quL - r1; //9.23
              
               quW = quL/lambda; // 9.24
              
               quWQ = quW - (1/mu); // 9.25
               
               return true;
                
               }

           if ( servers == 1 && K > 0 ) // MM1K - 1 SERVER - CAP GIVEN
                 {
              

                 tmpNum = ( 1 - ((K+1) * Math.Pow( r1, K )) + ( K * Math.Pow( r1, K + 1 ) ) ); //9.30 NUMERATOR
                 tmpDen = ( 1 - r1 ) * ( 1 - Math.Pow( r1, K + 1 ) ); // 9.30 DENUMERATOR

                 if ( tmpDen !=0 ) quL = r1 * ( tmpNum / tmpDen ); // 9.30
                
                 tmpDen =  lambda * ( 1 - pk ) ;
                 if ( tmpDen != 0 ) quW = quL / tmpDen; // 9.31
                 
                 quWQ = quW - ( 1 / mu ); // 9.32

                 quLQ = lambda * ( 1 - pk ) * quWQ;  // 9.33

                 return true;
                 }


           if ( servers > 1 && K > 0 ) // MMSK - >1 SERVER - CAP GIVEN (M/M/S/K)
                 {

                 tmpDen  = Math.Pow( 1 - r, 2 ) ; 
                 if (tmpDen != 0 ) quLQ = ( p0 * ps * Math.Pow( r, servers + 1 ) ) / tmpDen;
                 
                 quLQ =  quLQ * ( 1 - ( 1 + ( 1 - r )*( K - servers )*Math.Pow( r, K - servers ) ) ); // 9.37

                 quL = quLQ + r1 * ( 1 - pk );  // 9.38

                 quW = quLQ + ( 1 / mu ); // 9.39

                 quWQ = quLQ / ( lambda * ( 1 - pk ) ); // 9.40
                  
                 return true; // 
                 }


             return false;
        }//--- VERIFIED AND ALL RELATED FIUNCTIONS - 12/16/13- REG SHAUM PROB HSU P280-286


        
            
        // ------------
        //--- class end
        
        }

   
}